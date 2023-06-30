﻿using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using GitCommands;
using GitUIPluginInterfaces;

namespace GitUI.UserControls.RevisionGrid.Graph
{
    internal interface IRevisionGraphRowProvider
    {
        IRevisionGraphRow? GetSegmentsForRow(int row);
    }

    // The RevisionGraph contains all the basic structures needed to render the graph.
    public class RevisionGraph : IRevisionGraphRowProvider
    {
        internal const int MaxLanes = 40;
        private const int _orderSegmentsLookAhead = 50;
        private const int _straightenLanesLookAhead = 20;

        // Some unordered collections with raw data
        private ConcurrentDictionary<ObjectId, RevisionGraphRevision> _nodeByObjectId = new();
        private ImmutableList<RevisionGraphRevision> _nodes = ImmutableList<RevisionGraphRevision>.Empty;

        private bool _loadingCompleted;

        /// <summary>
        /// The max score is used to keep a chronological order during the graph building.
        /// It is cheaper than doing <c>_nodes.Max(n => n.Score)</c>.
        /// </summary>
        private int _maxScore;

        /// <summary>
        /// The node cache is an ordered list with the nodes.
        /// This is used so we can draw commits before the graph building is complete.
        /// </summary>
        /// <remarks>This cache is very cheap to build.</remarks>
        private RevisionGraphRevision[]? _orderedNodesCache;
        private bool _reorder = true;
        private int _orderedUntilScore = -1;

        /// <summary>
        /// The ordered row cache contains rows with segments stored in lanes.
        /// </summary>
        /// <remarks>This cache is very expensive to build.</remarks>
        private IList<RevisionGraphRow>? _orderedRowCache;

        private int _straightenDiagonalsLookAhead => AppSettings.StraightenGraphDiagonals.Value ? _straightenLanesLookAhead / 2 : 0;

        /// <summary>
        /// The number of rows needed to look ahead for straightening diagonals and lanes.
        /// Refer to the <cref>CacheTo</cref> function.
        /// </summary>
        private int _straightenLookAhead => 2 * (_straightenDiagonalsLookAhead + _straightenLanesLookAhead);

        // When the cache is updated, this action can be used to invalidate the UI
        public event Action? Updated;

        public void Clear()
        {
            _loadingCompleted = false;
            _maxScore = 0;
            _nodeByObjectId.Clear();
            _nodes = ImmutableList<RevisionGraphRevision>.Empty;
            _orderedNodesCache = null;
            _orderedRowCache = null;
        }

        public void LoadingCompleted()
        {
            _loadingCompleted = true;
        }

        public int Count => _nodes.Count;

        public bool OnlyFirstParent { get; set; }
        public ObjectId HeadId { get; set; }

        /// <summary>
        /// Checks whether the given hash is present in the graph.
        /// </summary>
        /// <param name="objectId">The hash to find.</param>
        /// <returns><see langword="true"/>, if the given hash if found; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="objectId"/> is <see langword="null"/>.</exception>
        public bool Contains(ObjectId objectId) => _nodeByObjectId.ContainsKey(objectId);

        public int GetCachedCount()
        {
            if (_orderedRowCache is null)
            {
                return 0;
            }

            // _loadingCompleted is set already when all _nodes have been loaded.
            // Return the full number of rows only if the straightening of segments has finished, too.
            // Else do not show rows yet which might be affected by the straightening of segments.
            int cachedCount = _orderedRowCache.Count;
            return _loadingCompleted && cachedCount == Count ? cachedCount : Math.Max(0, cachedCount - _straightenLookAhead);
        }

        /// <summary>
        /// Builds the revision graph cache. There are two caches that are built in this method.
        /// <para>Cache 1: an ordered list of the revisions. This is very cheap to build. (_orderedNodesCache).</para>
        /// <para>Cache 2: an ordered list of all prepared graph rows. This is expensive to build. (_orderedRowCache).</para>
        /// </summary>
        /// <param name="currentRowIndex">
        /// The row that needs to be displayed. This ensures the ordered revisions are available up to this index.
        /// </param>
        /// <param name="lastToCacheRowIndex">
        /// The graph can be built per x rows. This defines the last row index that the graph will build cache to.
        /// </param>
        public void CacheTo(int currentRowIndex, int lastToCacheRowIndex)
        {
            // Graph segments shall be straightened. For this, we need to look ahead some rows.
            // If lanes of a row are moved, go back the same number of rows as for the look-ahead
            // because then the previous rows could benefit from segment straightening, too.
            // Afterwards straighten diagonals. There is no significant need to look back for straightening lanes again - keeping it easy.
            //
            // row 0
            // row 1
            // ...
            // last finally straightened row                    <-- GetCachedCount()
            // first partially straightened diagonals row       <-- go back not further than here for straightening diagonals
            // ...
            // last partially straightened diagonals row
            // first look-ahead row for diagonals straightening
            // ...
            // last look-ahead row for diagonals straightening  <-- GetCachedCount() + 2 * _straightenDiagonalsLookAhead
            // first partially straightened lanes row           <-- go back not further than here for straightening lanes
            // ...
            // last partially straightened lanes row
            // row to continue lane straightening               <-- GetCachedCount() + 2 * _straightenDiagonalsLookAhead + _straightenLanesLookAhead
            // first look-ahead row for lane straightening
            // ...
            // last look-ahead row for lane straightening       <-- GetCachedCount() + 2 * _straightenDiagonalsLookAhead + 2 * _straightenLanesLookAhead
            //
            int lookAhead = _straightenLookAhead;
            currentRowIndex += lookAhead;
            lastToCacheRowIndex += lookAhead;

            if (_loadingCompleted)
            {
                int maxRowIndex = Count - 1;
                currentRowIndex = Math.Min(currentRowIndex, maxRowIndex);
                lastToCacheRowIndex = Math.Min(lastToCacheRowIndex, maxRowIndex);
            }

            RevisionGraphRevision[] orderedNodesCache = BuildOrderedNodesCache(currentRowIndex);

            BuildOrderedRowCache(orderedNodesCache, currentRowIndex, lastToCacheRowIndex);
        }

        public bool IsRowRelative(int row)
        {
            var node = GetNodeForRow(row);
            return node is not null && node.IsRelative;
        }

        public bool IsRevisionRelative(ObjectId objectId)
        {
            if (_nodeByObjectId.TryGetValue(objectId, out RevisionGraphRevision revision))
            {
                return revision.IsRelative;
            }

            return false;
        }

        public bool TryGetNode(ObjectId objectId, [NotNullWhen(true)] out RevisionGraphRevision? revision)
        {
            return _nodeByObjectId.TryGetValue(objectId, out revision);
        }

        public bool TryGetRowIndex(ObjectId objectId, out int index)
        {
            if (!TryGetNode(objectId, out RevisionGraphRevision revision))
            {
                index = 0;
                return false;
            }

            index = Array.IndexOf(BuildOrderedNodesCache(Count), revision);
            return index >= 0;
        }

        public RevisionGraphRevision? GetNodeForRow(int row)
        {
            // Use a local variable, because the cached list can be reset
            var localOrderedNodesCache = BuildOrderedNodesCache(row);
            if (row >= localOrderedNodesCache.Length)
            {
                return null;
            }

            return localOrderedNodesCache.ElementAt(row);
        }

        public IRevisionGraphRow? GetSegmentsForRow(int row)
        {
            // Use a local variable, because the cached list can be reset
            var localOrderedRowCache = _orderedRowCache;
            if (localOrderedRowCache is null || row < 0 || row >= localOrderedRowCache.Count)
            {
                return null;
            }

            return localOrderedRowCache[row];
        }

        public void HighlightBranch(ObjectId id)
        {
            // Clear current highlighting
            foreach (var revision in _nodes)
            {
                revision.IsRelative = false;
            }

            // Highlight revision
            if (TryGetNode(id, out RevisionGraphRevision revisionGraphRevision))
            {
                revisionGraphRevision.MakeRelative();
            }
        }

        /// <summary>
        /// Set HasNotes for all GitRevisions (marking Notes as fetched).
        /// This is used when no Git Notes at all exist and notes never need to be retrieved.
        /// </summary>
        public void SetHasNotesForRevisions()
        {
            foreach (RevisionGraphRevision revision in _nodes)
            {
                revision.GitRevision.HasNotes = true;
            }
        }

        /// <summary>
        /// Add a single revision from the git log to the graph, including segments to parents.
        /// </summary>
        /// <param name="revision">The revision to add.</param>
        /// <param name="insertScore">Insert the (artificial) revision before the node with this score.</param>
        /// <param name="insertRange">Number of scores "reserved" in the list when inserting.</param>
        public void Add(GitRevision revision, int? insertScore = null, int insertRange = 0)
        {
            // The commits are sorted by the score (not contiuous numbering there may be gaps)
            // This commit will be ordered after existing, _maxScore is a preliminary score
            _maxScore++;

            bool updateParents = true;
            if (!_nodeByObjectId.TryGetValue(revision.ObjectId, out RevisionGraphRevision revisionGraphRevision))
            {
                // This revision is added from the log, but not seen before. This is probably a root node (new branch)
                // OR the revisions are not in topo order. If this the case, we deal with it later.
                int score = _maxScore;

                if (insertScore is not null && _nodeByObjectId is not null)
                {
                    // This revision is to be inserted before a certain node
                    foreach (var (_, graphRevision) in _nodeByObjectId)
                    {
                        if (graphRevision.Score < insertScore)
                        {
                            // Lower existing scores to reserve the inserted range
                            graphRevision.OffsetScore(-insertRange);
                        }
                    }

                    score = insertScore.Value - insertRange;
                }

                revisionGraphRevision = new RevisionGraphRevision(revision.ObjectId, score);
                _nodeByObjectId.TryAdd(revision.ObjectId, revisionGraphRevision);
            }
            else
            {
                // This revision was added earlier, but is now found in the log.
                if (insertScore is null)
                {
                    // Increase the score to the current maxScore to keep the order intact.
                    revisionGraphRevision.EnsureScoreIsAbove(_maxScore);
                }
                else
                {
                    // Second artificial (Index), score already set
                    // No parent segment to be added (HEAD not in grid)
                    updateParents = false;
                }
            }

            // This revision may have been added as a parent before. Probably only the ObjectId is known. Set all the other properties.
            revisionGraphRevision.GitRevision = revision;
            revisionGraphRevision.ApplyFlags(isCheckedOut: HeadId == revision.ObjectId);

            // Build the revisions parent/child structure. The parents need to added here. The child structure is kept in synch in
            // the RevisionGraphRevision class.
            if (revision.ParentIds is not null && updateParents)
            {
                foreach (ObjectId parentObjectId in revision.ParentIds)
                {
                    if (!_nodeByObjectId.TryGetValue(parentObjectId, out RevisionGraphRevision parentRevisionGraphRevision))
                    {
                        int score = insertScore is not null

                            // Inserted after current revision
                            ? revisionGraphRevision.Score + 1 + revision.ParentIds.IndexOf(parentId => parentId == parentObjectId)

                            // This parent is not loaded before. Create a new (partial) revision. We will complete the info in the revision
                            // when this revision is loaded from the log.
                            : ++_maxScore;
                        parentRevisionGraphRevision = new RevisionGraphRevision(parentObjectId, score);
                        _nodeByObjectId.TryAdd(parentObjectId, parentRevisionGraphRevision);
                    }
                    else
                    {
                        // This revision is already loaded, add the existing revision to the parents list of new revision.
                        // If the current score is lower, cache is invalid. The new score will (probably) be higher.
                        MarkCacheAsInvalidIfNeeded(parentRevisionGraphRevision);
                    }

                    // Store the newly created segment (connection between 2 revisions)
                    revisionGraphRevision.AddParent(parentRevisionGraphRevision, out int newMaxScore);
                    _maxScore = Math.Max(_maxScore, newMaxScore);

                    if (OnlyFirstParent)
                    {
                        break;
                    }
                }
            }

            // Ensure all parents are loaded before adding it to the _nodes list. This is important for ordering.
            ImmutableInterlocked.Update(ref _nodes, (list, revision) => list.Add(revision), revisionGraphRevision);

            if (!updateParents)
            {
                // The rows may already be cached, invalidate and request reload of "some" rows
                _reorder = true;
                CacheTo(0, 99);
            }
        }

        /// <summary>
        /// It is very easy to check if the rowcache is dirty or not. If the last revision added to the rowcache
        /// is not in the same index in the orderednodecache, the order has been changed. Only then rebuilding is
        /// required. If the order is changed after this revision, we do not care since it wasn't processed yet.
        /// </summary>
        private static bool CheckRowCacheIsDirty(IList<RevisionGraphRow> orderedRowCache, RevisionGraphRevision[] orderedNodesCache)
        {
            // We need bounds checking on orderedNodesCache. It should be always larger then the rowcache,
            // but another thread could clear the orderedNodesCache while another is building orderedRowCache.
            // This is not a problem, since all methods use local instances of those caches. We do need to invalidate.
            if (orderedRowCache.Count > orderedNodesCache.Length)
            {
                return true;
            }

            if (orderedRowCache.Count == 0)
            {
                return false;
            }

            int indexToCompare = orderedRowCache.Count - 1;
            return orderedRowCache[indexToCompare].Revision != orderedNodesCache[indexToCompare];
        }

        private void BuildOrderedRowCache(RevisionGraphRevision[] orderedNodesCache, int currentRowIndex, int lastToCacheRowIndex)
        {
            bool orderSegments = AppSettings.ReduceGraphCrossings.Value;

            int lastOrderedNodeIndex = orderedNodesCache.Length - 1;

            // Ensure we keep using the same instance of the rowcache from here on
            bool loadingCompleted = _loadingCompleted;
            IList<RevisionGraphRow>? localOrderedRowCache = _orderedRowCache;

            int maxLastToCacheRowIndex = lastOrderedNodeIndex - (loadingCompleted || !orderSegments ? 0 : _orderSegmentsLookAhead);
            if (lastToCacheRowIndex > maxLastToCacheRowIndex)
            {
                lastToCacheRowIndex = maxLastToCacheRowIndex;
                loadingCompleted = false;
            }

            if (localOrderedRowCache is null || CheckRowCacheIsDirty(localOrderedRowCache, orderedNodesCache))
            {
                localOrderedRowCache = new List<RevisionGraphRow>(capacity: Math.Max(currentRowIndex, lastOrderedNodeIndex) + 1);
            }

            int startIndex = localOrderedRowCache.Count;
            if (startIndex > lastToCacheRowIndex)
            {
                return;
            }

            for (int nextIndex = startIndex; nextIndex <= lastToCacheRowIndex; ++nextIndex)
            {
                RevisionGraphRevision revision = orderedNodesCache[nextIndex];
                RevisionGraphSegment[] revisionStartSegments = revision.GetStartSegments();
                if (orderSegments)
                {
                    revisionStartSegments = Order(revisionStartSegments, orderedNodesCache, nextIndex);
                }

                // The list containing the segments is created later. We can set the correct capacity then, to prevent resizing
                List<RevisionGraphSegment> segments;

                RevisionGraphRow? previousRevisionGraphRow;
                if (nextIndex == 0)
                {
                    previousRevisionGraphRow = null;

                    // This is the first row. Start with only the startsegments of this row
                    segments = new List<RevisionGraphSegment>(revisionStartSegments);

                    RevisionGraphSegment? prevSegment = null;
                    foreach (RevisionGraphSegment startSegment in revisionStartSegments)
                    {
                        startSegment.LaneInfo = new LaneInfo(startSegment, derivedFrom: null, prevSegment);
                        prevSegment = startSegment;
                    }
                }
                else
                {
                    // Copy lanes from last row
                    previousRevisionGraphRow = localOrderedRowCache[nextIndex - 1];

                    // Create segments list with the correct capacity
                    segments = new List<RevisionGraphSegment>(previousRevisionGraphRow.Segments.Count + revisionStartSegments.Length);

                    bool startSegmentsAdded = false;

                    // Loop through all segments that do not end in the previous row
                    foreach (var segment in previousRevisionGraphRow.Segments.Where(s => s.Parent != previousRevisionGraphRow.Revision))
                    {
                        segments.Add(segment);

                        // This segment that is copied from the previous row, connects to the node in this row.
                        // Copy all new segments that start from this node (revision) to this lane.
                        if (revision == segment.Parent)
                        {
                            RevisionGraphSegment prevSegment = segments.LastOrDefault();

                            if (!startSegmentsAdded)
                            {
                                startSegmentsAdded = true;
                                segments.AddRange(revisionStartSegments);
                            }

                            foreach (RevisionGraphSegment startSegment in revisionStartSegments)
                            {
                                if (startSegment == revisionStartSegments[0])
                                {
                                    if (startSegment.LaneInfo is null || startSegment.LaneInfo.Value.StartScore > segment.LaneInfo?.StartScore)
                                    {
                                        startSegment.LaneInfo = segment.LaneInfo;
                                    }
                                }
                                else
                                {
                                    startSegment.LaneInfo ??= new LaneInfo(startSegment, derivedFrom: segment.LaneInfo, prevSegment);
                                }

                                prevSegment = startSegment;
                            }
                        }
                    }

                    // The startsegments do not connect to any previous row. This means that this is a new branch.
                    if (!startSegmentsAdded)
                    {
                        RevisionGraphSegment prevSegment = segments.LastOrDefault();

                        // Add new segments started by this revision to the end
                        segments.AddRange(revisionStartSegments);

                        foreach (RevisionGraphSegment startSegment in revisionStartSegments)
                        {
                            startSegment.LaneInfo = new LaneInfo(startSegment, derivedFrom: null, prevSegment);
                            prevSegment = startSegment;
                        }
                    }
                }

                localOrderedRowCache.Add(new RevisionGraphRow(revision, segments, previousRevisionGraphRow));
            }

            // Straightening does not apply to the first and the last row. The single node there shall not be moved.
            // So the straightening algorithm can presume that a previous and a next row do exist.
            // Straighten only lines for which the full look-ahead is loaded.
            loadingCompleted = loadingCompleted && lastToCacheRowIndex == lastOrderedNodeIndex;
            int straightenLanesStartIndex = Math.Max(1, startIndex - _straightenLanesLookAhead);
            int straightenLanesLastIndex = loadingCompleted ? lastToCacheRowIndex - 1 : lastToCacheRowIndex - _straightenLanesLookAhead;
            StraightenLanes(straightenLanesStartIndex, straightenLanesLastIndex, lastLookAheadIndex: lastToCacheRowIndex, localOrderedRowCache);

            int straightenDiagonalsLookAhead = _straightenDiagonalsLookAhead;
            if (straightenDiagonalsLookAhead > 0)
            {
                int straightenDiagonalsStartIndex = Math.Max(1, startIndex - _straightenLanesLookAhead - straightenDiagonalsLookAhead);
                int straightenDiagonalsLastIndex = loadingCompleted ? lastToCacheRowIndex - 1 : lastToCacheRowIndex - _straightenLanesLookAhead - straightenDiagonalsLookAhead;
                StraightenDiagonals(straightenDiagonalsStartIndex, straightenDiagonalsLastIndex, lastLookAheadIndex: lastToCacheRowIndex, straightenDiagonalsLookAhead, localOrderedRowCache);
            }

            // Overwrite the global instance at the end, to prevent flickering
            _orderedRowCache = localOrderedRowCache;

            Updated?.Invoke();

            return;

            static RevisionGraphSegment[] Order(RevisionGraphSegment[] segments, RevisionGraphRevision[] orderedNodesCache, int nextIndex)
            {
                // Define local function GetRowIndex with precalculated limit here
                int endIndex = Math.Min(nextIndex + _orderSegmentsLookAhead, orderedNodesCache.Length);
                int GetRowIndex(RevisionGraphRevision revision)
                {
                    for (int index = nextIndex + 1; index < endIndex; ++index)
                    {
                        if (orderedNodesCache[index] == revision)
                        {
                            return index - nextIndex;
                        }
                    }

                    return int.MaxValue;
                }

                return segments.OrderBy(s => s, (a, b) =>
                    {
                        int rowA = GetRowIndex(a.Parent);
                        int rowB = GetRowIndex(b.Parent);

                        // Prefer the one which is the ancestor of the other
                        if (rowA != int.MaxValue && rowB != int.MaxValue)
                        {
                            if (rowA > rowB && IsAncestorOf(a.Parent, b.Parent, rowA))
                            {
                                return -1;
                            }
                            else if (rowB > rowA && IsAncestorOf(b.Parent, a.Parent, rowB))
                            {
                                return 1;
                            }
                        }

                        return Score(a, rowA).CompareTo(Score(b, rowB));

                        int Score(RevisionGraphSegment segment, int row)
                            => segment.Parent.Parents.IsEmpty ? row // initial revision
                                : !segment.Parent.Parents.Pop().IsEmpty ? -2000 + row // merged into
                                : !segment.Parent.Children.Pop().IsEmpty ? -1000 + row // branched from
                                : row; // just a commit

                        bool IsAncestorOf(RevisionGraphRevision ancestor, RevisionGraphRevision child, int stopRow)
                        {
                            if (child.Parents.Contains(ancestor))
                            {
                                return true;
                            }

                            foreach (RevisionGraphRevision parent in child.Parents)
                            {
                                if (GetRowIndex(parent) < stopRow && IsAncestorOf(ancestor, parent, stopRow))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    })
                    .ToArray();
            }

            static void StraightenLanes(int startIndex, int lastStraightenIndex, int lastLookAheadIndex, IList<RevisionGraphRow> localOrderedRowCache)
            {
                // Try to detect this:
                // | | |<-- previous lane
                // |/ /
                // * |<---- current lane
                // | |
                // | |
                // | |
                // |\ \
                // | | |<-- look-ahead lane
                //
                // And change it into this:
                // | | |
                // |/  |
                // *   |
                // |   |
                // |   |
                // |   |
                // |\  |
                // | | |
                //
                // also if the distance is > 1 but only if the other distance is exactly 1

                int goBackLimit = 1;
                for (int currentIndex = startIndex; currentIndex <= lastStraightenIndex;)
                {
                    goBackLimit = Math.Max(goBackLimit, currentIndex - _straightenLanesLookAhead);
                    bool moved = false;
                    IRevisionGraphRow currentRow = localOrderedRowCache[currentIndex];
                    IRevisionGraphRow previousRow = localOrderedRowCache[currentIndex - 1];
                    foreach (RevisionGraphSegment revisionGraphSegment in currentRow.Segments.Take(MaxLanes))
                    {
                        Lane currentRowLane = currentRow.GetLaneForSegment(revisionGraphSegment);
                        if (currentRowLane.Sharing != LaneSharing.ExclusiveOrPrimary)
                        {
                            continue; // with next revisionGraphSegment
                        }

                        int currentLane = currentRowLane.Index;
                        int previousLane = previousRow.GetLaneForSegment(revisionGraphSegment).Index;
                        if (previousLane <= currentLane)
                        {
                            continue; // with next revisionGraphSegment
                        }

                        int straightenedCurrentLane = currentLane + 1;
                        int lookAheadLane = currentLane;
                        RevisionGraphSegment segmentOrAncestor = currentRow.FirstParentOrSelf(revisionGraphSegment);
                        for (int lookAheadIndex = currentIndex + 1; lookAheadLane == currentLane && lookAheadIndex <= Math.Min(currentIndex + _straightenLanesLookAhead, lastLookAheadIndex); ++lookAheadIndex)
                        {
                            RevisionGraphRow lookAheadRow = localOrderedRowCache[lookAheadIndex];
                            lookAheadLane = lookAheadRow.GetLaneForSegment(segmentOrAncestor).Index;
                            if ((lookAheadLane == straightenedCurrentLane) || (lookAheadLane > straightenedCurrentLane && previousLane == straightenedCurrentLane))
                            {
                                for (int moveIndex = currentIndex; moveIndex < lookAheadIndex; ++moveIndex)
                                {
                                    localOrderedRowCache[moveIndex].MoveLanesRight(currentLane);
                                }

                                moved = true;
                                break; // from for lookAheadIndex
                            }

                            segmentOrAncestor = lookAheadRow.FirstParentOrSelf(segmentOrAncestor);
                        }

                        if (moved)
                        {
                            break; // from for revisionGraphSegment
                        }
                    }

                    // if moved, check again whether the lanes of previous rows can be moved, too
                    currentIndex = moved ? Math.Max(currentIndex - _straightenLanesLookAhead, goBackLimit) : currentIndex + 1;
                }
            }

            static void StraightenDiagonals(int startIndex, int lastStraightenIndex, int lastLookAheadIndex, int straightenDiagonalsLookAhead, IList<RevisionGraphRow> localOrderedRowCache)
            {
                List<MoveLaneBy> moveLaneBy = new(capacity: straightenDiagonalsLookAhead);
                int goBackLimit = 1;
                for (int currentIndex = startIndex; currentIndex <= lastStraightenIndex;)
                {
                    goBackLimit = Math.Max(goBackLimit, currentIndex - straightenDiagonalsLookAhead);
                    int currentLastLookAheadIndex = Math.Min(currentIndex + straightenDiagonalsLookAhead, lastLookAheadIndex);
                    bool moved = false;
                    IRevisionGraphRow currentRow = localOrderedRowCache[currentIndex];
                    IRevisionGraphRow previousRow = localOrderedRowCache[currentIndex - 1];
                    foreach (RevisionGraphSegment revisionGraphSegment in currentRow.Segments.Take(MaxLanes))
                    {
                        Lane currentRowLane = currentRow.GetLaneForSegment(revisionGraphSegment);
                        if (currentRowLane.Sharing != LaneSharing.ExclusiveOrPrimary)
                        {
                            continue; // with next revisionGraphSegment
                        }

                        int currentLane = currentRowLane.Index;
                        int previousLane = previousRow.GetLaneForSegment(revisionGraphSegment).Index;

                        // Unfold one-lane shift to diagonal (cannot be done together with TurnMultiLaneCrossingIntoDiagonal(diagonalDelta: -1))
                        // Try to detect this:
                        // * | | <-- previous lane
                        // |/ /
                        // * | <---- current lane
                        // | |
                        // * | <---- next lane
                        // |/
                        // * <------ end lane
                        //
                        // And change it into this:
                        // * | | <-- previous lane
                        // |/  |
                        // *   | <-- current lane
                        // |  /
                        // * / <---- next lane
                        // |/
                        // * <------ end lane
                        if (currentLane == previousLane - 1 && currentIndex + 2 <= currentLastLookAheadIndex)
                        {
                            RevisionGraphSegment segmentOrAncestor = currentRow.FirstParentOrSelf(revisionGraphSegment);
                            IRevisionGraphRow nextRow = localOrderedRowCache[currentIndex + 1];
                            int nextLane = nextRow.GetLaneForSegment(segmentOrAncestor).Index;
                            if (nextLane == currentLane)
                            {
                                segmentOrAncestor = nextRow.FirstParentOrSelf(segmentOrAncestor);
                                int endLane = localOrderedRowCache[currentIndex + 2].GetLaneForSegment(segmentOrAncestor).Index;
                                if (endLane >= 0 && endLane == nextLane - 1 && !IsPrevLaneDiagonal())
                                {
                                    currentRow.MoveLanesRight(currentLane);
                                    ++currentLane;
                                    moved = true;
                                    break; // from for revisionGraphSegment
                                }
                            }
                        }

                        moved = TurnMultiLaneCrossingIntoDiagonal(diagonalDelta: +1)
                              || TurnMultiLaneCrossingIntoDiagonal(diagonalDelta: -1);
                        if (moved)
                        {
                            break; // from for revisionGraphSegment
                        }

                        // Join multi-lane crossings
                        // Try to detect this:
                        // | | * | <-- previous lane (not diagonal)
                        // | |/ /
                        // | * | <---- current lane
                        // |,-´
                        // | <-------- next lane (not diagonal)
                        //
                        // And change it into this:
                        // | | * | <-- previous lane
                        // | |/  |
                        // | *   | <-- current lane
                        // |,---´
                        // | <-------- next lane
                        int deltaPrev = previousLane - currentLane;
                        if (previousLane >= 0 && Math.Abs(deltaPrev) >= 1)
                        {
                            RevisionGraphSegment segmentOrAncestor = currentRow.FirstParentOrSelf(revisionGraphSegment);
                            IRevisionGraphRow nextRow = localOrderedRowCache[currentIndex + 1];
                            int nextLane = nextRow.GetLaneForSegment(segmentOrAncestor).Index;
                            int deltaNext = currentLane - nextLane;
                            if (nextLane >= 0 && Math.Sign(deltaNext) == Math.Sign(deltaPrev) && Math.Abs(deltaNext + deltaPrev) >= 3 && !IsPrevLaneDiagonal(Math.Sign(deltaPrev)) && !IsNextLaneDiagonal())
                            {
                                int moveBy = deltaNext < 0 ? -deltaNext : deltaPrev;
                                currentRow.MoveLanesRight(currentLane, moveBy);
                                currentLane += moveBy;
                                moved = true;
                                break; // from for revisionGraphSegment
                            }

                            bool IsNextLaneDiagonal()
                            {
                                if (currentIndex + 2 > currentLastLookAheadIndex)
                                {
                                    return false;
                                }

                                segmentOrAncestor = nextRow.FirstParentOrSelf(segmentOrAncestor);
                                int nextNextLane = localOrderedRowCache[currentIndex + 2].GetLaneForSegment(segmentOrAncestor).Index;
                                return nextNextLane >= 0 && nextNextLane == nextLane - Math.Sign(deltaNext);
                            }
                        }

                        continue; // for revisionGraphSegment (added just as a separator for the local function)

                        bool IsPrevLaneDiagonal(int diagonalDelta = 1)
                        {
                            if (currentIndex < 2)
                            {
                                return false;
                            }

                            int prevPrevLane = localOrderedRowCache[currentIndex - 2].GetLaneForSegment(revisionGraphSegment).Index;
                            return prevPrevLane >= 0 && prevPrevLane == previousLane + diagonalDelta;
                        }

                        // Turn multi-lane crossings into diagonals
                        // Try to detect this:
                        // * | <------ previous lane
                        // | |
                        // * | <------ current lane
                        // |\-.-.
                        // * | | | <-- end lane
                        //
                        // And change it into this:
                        // * | <------ previous lane
                        // |  \
                        // *   \ <---- current lane
                        // |\-. \
                        // * | | | <-- end lane
                        //
                        // Try to detect this:
                        // * | | | <-- previous lane
                        // |/-´-´
                        // * | <------ current lane
                        // | |
                        // * | <------ end lane
                        //
                        // And change it into this:
                        // * | | | <-- previous lane
                        // |/-´ /
                        // *   / <---- current lane
                        // |  /
                        // * | <------ end lane
                        bool TurnMultiLaneCrossingIntoDiagonal(int diagonalDelta)
                        {
                            moveLaneBy.Clear();
                            RevisionGraphSegment segmentOrAncestor = revisionGraphSegment;
                            int diagonalLane = previousLane >= 0 ? previousLane : currentLane;
                            for (int lookAheadIndex = currentIndex; lookAheadIndex <= currentLastLookAheadIndex; ++lookAheadIndex)
                            {
                                diagonalLane += diagonalDelta;
                                IRevisionGraphRow endRow = localOrderedRowCache[lookAheadIndex];
                                Lane endLane = endRow.GetLaneForSegment(segmentOrAncestor);
                                int moveBy = diagonalLane - endLane.Index;
                                bool lastChance = endLane.Sharing == LaneSharing.DifferentStart;
                                if (moveBy < 0 || endLane.Index < 0 || !(endLane.Sharing == LaneSharing.ExclusiveOrPrimary || lastChance))
                                {
                                    return false;
                                }

                                // Unfold one-lane shift to diagonal, too
                                // Try to detect this:
                                // * <------ previous lane
                                // |\
                                // * | <---- current lane
                                // | |
                                // * | <---- move lane
                                // |\ \
                                // * | | <-- keep lane
                                // | | |
                                // * | | <-- end lane
                                //
                                // And change it into this:
                                // * <------ previous lane
                                // |\
                                // * \ <---- current lane
                                // |  \
                                // *   | <-- move lane
                                // |\  |
                                // * | | <-- keep lane with moveBy == 1
                                // | | |
                                // * | | <-- end lane with moveBy >= 2
                                if (moveBy >= 2 && moveLaneBy.Count == 2 && lookAheadIndex == currentIndex + 3 && moveLaneBy[1].By == 1)
                                {
                                    MoveLanes(moveLaneBy.Take(1));
                                    return true;
                                }

                                // Diagonal ends
                                if (moveBy == 0 && moveLaneBy.Count > 0)
                                {
                                    MoveLanes(moveLaneBy);
                                    return true;
                                }

                                if (lastChance)
                                {
                                    return false;
                                }

                                if (moveBy > 0)
                                {
                                    moveLaneBy.Add(new MoveLaneBy(endRow, endLane.Index, moveBy));
                                }

                                segmentOrAncestor = endRow.FirstParentOrSelf(segmentOrAncestor);
                            }

                            return false;

                            static void MoveLanes(IEnumerable<MoveLaneBy> moveLaneBy)
                            {
                                foreach ((IRevisionGraphRow row, int lane, int by) in moveLaneBy)
                                {
                                    row.MoveLanesRight(lane, by);
                                }
                            }
                        }
                    }

                    // if moved, check again whether the lanes of previous rows can be moved, too
                    currentIndex = moved ? Math.Max(currentIndex - straightenDiagonalsLookAhead, goBackLimit) : currentIndex + 1;
                }
            }
        }

        private RevisionGraphRevision[] BuildOrderedNodesCache(int currentRowIndex)
        {
            if (_orderedNodesCache is not null && !_reorder && _orderedNodesCache.Length >= Math.Min(Count, currentRowIndex))
            {
                return _orderedNodesCache;
            }

            // The Score of the referenced nodes can be rewritten by another thread.
            // Then the cache is invalidated by setting _reorder.
            // So if Sort() complains in this case, try again.
            while (true)
            {
                try
                {
                    // Reset the reorder flag and the orderedUntilScore. This makes sure it isn't marked dirty before we even got to
                    // rebuilding it.
                    _orderedUntilScore = int.MinValue;
                    _reorder = false;

                    // Use a local variable, because the cached list can be reset.
                    RevisionGraphRevision[] localOrderedNodesCache = _nodes.ToArray();
                    Array.Sort(localOrderedNodesCache, (x, y) => x.Score.CompareTo(y.Score));
                    _orderedNodesCache = localOrderedNodesCache;
                    if (localOrderedNodesCache.Length > 0)
                    {
                        _orderedUntilScore = localOrderedNodesCache.Last().Score;
                    }

                    return localOrderedNodesCache;
                }
                catch (ArgumentException ex) when (_reorder && ex.Message.Contains("IComparer.Compare()"))
                {
                    // ignore and try again
                }
            }
        }

        private void MarkCacheAsInvalidIfNeeded(RevisionGraphRevision revisionGraphRevision)
        {
            if (revisionGraphRevision.Score <= _orderedUntilScore)
            {
                _reorder = true;
            }
        }

        private readonly record struct MoveLaneBy(IRevisionGraphRow Row, int Lane, int By);

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly RevisionGraph _revisionGraph;

            public TestAccessor(RevisionGraph revisionGraph)
            {
                _revisionGraph = revisionGraph;
            }

            // This method will validate the topo order in brute force.
            // Only used for unit testing.
            public bool ValidateTopoOrder()
            {
                foreach (var node in _revisionGraph._nodes)
                {
                    foreach (var parent in node.Parents)
                    {
                        if (parent.Score <= node.Score)
                        {
                            return false;
                        }
                    }

                    foreach (var child in node.Children)
                    {
                        if (node.Score <= child.Score)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}
