using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitExtUtils;
using GitUI;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitCommands
{
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
    [Flags]
    public enum RefFilterOptions
    {
        None                    = 0x000,
        Branches                = 0x001,    // --branches
        Remotes                 = 0x002,    // --remotes
        Tags                    = 0x004,    // --tags
        Stashes                 = 0x008,    //
        All                     = 0x00F,    // --all
        Boundary                = 0x010,    // --boundary
        ShowGitNotes            = 0x020,    // --not --glob=notes --not
        NoMerges                = 0x040,    // --no-merges
        FirstParent             = 0x080,    // --first-parent
        SimplifyByDecoration    = 0x100,    // --simplify-by-decoration
        Reflogs                 = 0x200,    // --reflog
    }
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row

    public sealed class RevisionReader : IDisposable
    {
        private const string FullFormat =

            // These header entries can all be decoded from the bytes directly.
            // Each hash is 20 bytes long.

            /* Object ID       */ "%H" +
            /* Tree ID         */ "%T" +
            /* Parent IDs      */ "%P%n" +
            /* Author date     */ "%at%n" +
            /* Commit date     */ "%ct%n" +
            /* Encoding        */ "%e%n" +

            // Items below here must be decoded as strings to support non-ASCII.
            /* Author name     */ "%aN%n" +
            /* Author email    */ "%aE%n" +
            /* Committer name  */ "%cN%n" +
            /* Committer email */ "%cE%n" +

            // Raw body, followed by additional data if path filter
            /* Commit raw body */ "%B";

        // Separate commit message body and filtered file names
        private const string EndOfBody = "1DEA7CC4-FB39-450A-8DDF-762FCEA28B05";
        private readonly CancellationTokenSequence _cancellationTokenSequence = new();

        public void Execute(
            GitModule module,
            IReadOnlyList<IGitRef> refs,
            IObserver<GitRevision> subject,
            RefFilterOptions refFilterOptions,
            string branchFilter,
            string revisionFilter,
            string pathFilter,
            Func<GitRevision, bool>? revisionPredicate)
        {
            ThreadHelper.JoinableTaskFactory
                .RunAsync(() => ExecuteAsync(module, refs, subject, refFilterOptions, branchFilter, revisionFilter, pathFilter, revisionPredicate))
                .FileAndForget(
                    ex =>
                    {
                        subject.OnError(ex);
                        return false;
                    });
        }

        private async Task ExecuteAsync(
            GitModule module,
            IReadOnlyList<IGitRef> refs,
            IObserver<GitRevision> subject,
            RefFilterOptions refFilterOptions,
            string branchFilter,
            string revisionFilter,
            string pathFilter,
            Func<GitRevision, bool>? revisionPredicate)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var token = _cancellationTokenSequence.Next();

            var revisionCount = 0;

            await TaskScheduler.Default;

            token.ThrowIfCancellationRequested();

            var branchName = module.IsValidGitWorkingDir()
                ? module.GetSelectedBranch()
                : "";

            token.ThrowIfCancellationRequested();

            UpdateSelectedRef(module, refs, branchName);
            var refsByObjectId = refs.ToLookup(head => head.ObjectId);

            token.ThrowIfCancellationRequested();

            bool hasPathFilter = !string.IsNullOrWhiteSpace(pathFilter);
            var arguments = BuildArguments(refFilterOptions, branchFilter, revisionFilter, pathFilter, hasPathFilter);

#if TRACE
            var sw = Stopwatch.StartNew();
#endif

            // This property is relatively expensive to call for every revision, so
            // cache it for the duration of the loop.
            var logOutputEncoding = module.LogOutputEncoding;

            using (var process = module.GitCommandRunner.RunDetached(arguments, redirectOutput: true, outputEncoding: GitModule.LosslessEncoding))
            {
                token.ThrowIfCancellationRequested();

                // Pool string values likely to form a small set: encoding, authorname, authoremail, committername, committeremail
                StringPool stringPool = new();

                var buffer = new byte[4096];

                foreach (var chunk in process.StandardOutput.BaseStream.ReadNullTerminatedChunks(ref buffer))
                {
                    token.ThrowIfCancellationRequested();

                    if (TryParseRevision(module, chunk, stringPool, logOutputEncoding, hasPathFilter, out var revision))
                    {
                        if (revisionPredicate is null || revisionPredicate(revision))
                        {
                            // Look up any refs associated with this revision
                            revision.Refs = refsByObjectId[revision.ObjectId].AsReadOnlyList();

                            revisionCount++;

                            subject.OnNext(revision);
                        }
                    }
                }

#if TRACE
                Trace.WriteLine($"**** [{nameof(RevisionReader)}] Emitted {revisionCount} revisions in {sw.Elapsed.TotalMilliseconds:#,##0.#} ms. bufferSize={buffer.Length} poolCount={stringPool.Count}");
#endif
            }

            if (!token.IsCancellationRequested)
            {
                subject.OnCompleted();
            }
        }

        private ArgumentBuilder BuildArguments(RefFilterOptions refFilterOptions,
            string branchFilter,
            string revisionFilter,
            string pathFilter,
            bool hasPathFilter)
        {
            string endFormat = hasPathFilter ? EndOfBody : "";
            return new GitArgumentBuilder("log")
            {
                "-z",
                {
                    !string.IsNullOrWhiteSpace(branchFilter) && IsSimpleBranchFilter(branchFilter),
                    branchFilter
                },
                $"--pretty=format:\"{FullFormat}{endFormat}\"",
                {
                    refFilterOptions.HasFlag(RefFilterOptions.FirstParent),
                    "--first-parent",
                    new ArgumentBuilder
                    {
                        { refFilterOptions.HasFlag(RefFilterOptions.Reflogs), "--reflog" },
                        { AppSettings.SortByAuthorDate, "--author-date-order" },
                        {
                            refFilterOptions.HasFlag(RefFilterOptions.All),
                            "--all",
                            new ArgumentBuilder
                            {
                                {
                                    refFilterOptions.HasFlag(RefFilterOptions.Branches) &&
                                    !string.IsNullOrWhiteSpace(branchFilter) && !IsSimpleBranchFilter(branchFilter),
                                    "--branches=" + branchFilter
                                },
                                { refFilterOptions.HasFlag(RefFilterOptions.Remotes), "--remotes" },
                                { refFilterOptions.HasFlag(RefFilterOptions.Tags), "--tags" },
                            }.ToString()
                        },
                        { refFilterOptions.HasFlag(RefFilterOptions.Boundary), "--boundary" },
                        { refFilterOptions.HasFlag(RefFilterOptions.ShowGitNotes), "--not --glob=notes --not" },
                        { refFilterOptions.HasFlag(RefFilterOptions.NoMerges), "--no-merges" },
                        { refFilterOptions.HasFlag(RefFilterOptions.SimplifyByDecoration), "--simplify-by-decoration" }
                    }.ToString()
                },
                revisionFilter,
                { hasPathFilter, $"-- {pathFilter}" }
            };
        }

        private static bool IsSimpleBranchFilter(string branchFilter) =>
            branchFilter.IndexOfAny(new[] { '?', '*', '[' }) == -1;

        private static void UpdateSelectedRef(GitModule module, IReadOnlyList<IGitRef> refs, string branchName)
        {
            var selectedRef = refs.FirstOrDefault(head => head.Name == branchName);

            if (selectedRef is not null)
            {
                selectedRef.IsSelected = true;

                var localConfigFile = module.LocalConfigFile;
                var selectedHeadMergeSource = refs.FirstOrDefault(
                    head => head.IsRemote
                         && selectedRef.GetTrackingRemote(localConfigFile) == head.Remote
                         && selectedRef.GetMergeWith(localConfigFile) == head.LocalName);

                if (selectedHeadMergeSource is not null)
                {
                    selectedHeadMergeSource.IsSelectedHeadMergeSource = true;
                }
            }
        }

        private static bool TryParseRevision(GitModule module, ArraySegment<byte> chunk, StringPool stringPool, Encoding logOutputEncoding, bool hasPathFilter, [NotNullWhen(returnValue: true)] out GitRevision? revision)
        {
            // The 'chunk' of data contains a complete git log item, encoded.
            // This method decodes that chunk and produces a revision object.

            // All values which can be read directly from the byte array are arranged
            // at the beginning of the chunk. The latter part of the chunk will require
            // decoding as a string.

            if (chunk.Count == 0)
            {
                // "git log -z --name-only" returns multiple consecutive null bytes when logging
                // the history of a single file. Haven't worked out why, but it's safe to skip
                // such chunks.
                revision = default;
                return false;
            }

            #region Object ID, Tree ID, Parent IDs

            // The first 40 bytes are the revision ID and the tree ID back to back
            if (!ObjectId.TryParseAsciiHexBytes(chunk, 0, out var objectId) ||
                !ObjectId.TryParseAsciiHexBytes(chunk, ObjectId.Sha1CharCount, out var treeId))
            {
                revision = default;
                return false;
            }

            var array = chunk.Array;
            var offset = chunk.Offset + (ObjectId.Sha1CharCount * 2);
            var lastOffset = chunk.Offset + chunk.Count;

            // Next we have zero or more parent IDs separated by ' ' and terminated by '\n'
            var parentIds = new ObjectId[CountParents(offset)];
            var parentIndex = 0;

            int CountParents(int baseOffset)
            {
                if (array[baseOffset] == '\n')
                {
                    return 0;
                }

                var count = 1;

                while (true)
                {
                    baseOffset += ObjectId.Sha1CharCount;
                    var c = array[baseOffset];

                    if (c != ' ')
                    {
                        break;
                    }

                    count++;
                    baseOffset++;
                }

                return count;
            }

            while (true)
            {
                if (offset >= lastOffset - ObjectId.Sha1CharCount - 1)
                {
                    revision = default;
                    return false;
                }

                var b = array[offset];

                if (b == '\n')
                {
                    // There are no more parent IDs
                    offset++;
                    break;
                }

                if (b == ' ')
                {
                    // We are starting a new parent ID
                    offset++;
                }

                if (!ObjectId.TryParseAsciiHexBytes(array, offset, out var parentId))
                {
                    // TODO log this parse problem
                    revision = default;
                    return false;
                }

                parentIds[parentIndex++] = parentId;
                offset += ObjectId.Sha1CharCount;
            }

            #endregion

            #region Timestamps

            // Lines 2 and 3 are timestamps, as decimal ASCII seconds since the unix epoch, each terminated by `\n`
            var authorDate = ParseUnixDateTime();
            var commitDate = ParseUnixDateTime();

            DateTime ParseUnixDateTime()
            {
                long unixTime = 0;

                while (true)
                {
                    var c = array[offset++];

                    if (c == '\n')
                    {
                        return DateTimeUtils.UnixEpoch.AddTicks(unixTime * TimeSpan.TicksPerSecond).ToLocalTime();
                    }

                    unixTime = (unixTime * 10) + (c - '0');
                }
            }

            #endregion

            #region Encoding

            // Line is the name of the encoding used by git, or an empty string, terminated by `\n`
            string? encodingName;
            Encoding encoding;

            var encodingNameEndOffset = Array.IndexOf(array, (byte)'\n', offset);

            if (encodingNameEndOffset == -1)
            {
                // TODO log this error case
                revision = default;
                return false;
            }

            if (offset == encodingNameEndOffset)
            {
                // No encoding specified
                encoding = logOutputEncoding;
                encodingName = null;
            }
            else
            {
                encodingName = logOutputEncoding.GetString(array, offset, encodingNameEndOffset - offset);
                encoding = module.GetEncodingByGitName(encodingName) ?? Encoding.UTF8;
            }

            offset = encodingNameEndOffset + 1;

            #endregion

            #region Encoded string values (names, emails, subject, body, [file]name)

            // Finally, decode the names, email, subject and body strings using the required text encoding
            var s = encoding.GetString(array, offset, lastOffset - offset);

            StringLineReader reader = new(s);

            var author = reader.ReadLine(stringPool);
            var authorEmail = reader.ReadLine(stringPool);
            var committer = reader.ReadLine(stringPool);
            var committerEmail = reader.ReadLine(stringPool);

            // We keep a full multiline message body within the last six months.
            // Note also that if body and subject are identical (single line), the subject never need to be stored
            bool skipBody = DateTime.Now - authorDate > TimeSpan.FromDays(30 * 6);
            reader.TrimStart();
            (string? subject, string? body, bool hasMultiLineMessage) = reader.ReadToEndOfBody(skipBody, hasPathFilter);

            if (author is null || authorEmail is null || committer is null || committerEmail is null || subject is null || (!skipBody && hasMultiLineMessage && body is null))
            {
                // TODO log this parse error
                Debug.Fail("Unable to read an entry from the log -- this should not happen");
                revision = default;
                return false;
            }

            var additionalData = reader.TrimStart().PeakToEnd();

            #endregion

            revision = new GitRevision(objectId)
            {
                ParentIds = parentIds,
                TreeGuid = treeId,
                Author = author,
                AuthorEmail = authorEmail,
                AuthorDate = authorDate,
                Committer = committer,
                CommitterEmail = committerEmail,
                CommitDate = commitDate,
                MessageEncoding = encodingName,
                Subject = subject,
                Body = body,
                Name = additionalData,
                HasMultiLineMessage = hasMultiLineMessage,
                HasNotes = false
            };

            return true;
        }

        public void Dispose()
        {
            _cancellationTokenSequence.Dispose();
        }

        #region Nested type: StringLineReader

        /// <summary>
        /// Simple type to walk along a string, line by line, without redundant allocations.
        /// </summary>
        internal struct StringLineReader
        {
            private readonly string _s;
            private int _index;

            public StringLineReader(string s)
            {
                _s = s;
                _index = 0;
            }

            public int Remaining => _s.Length - _index;

            public string? ReadLine(StringPool? pool = null)
            {
                if (_index == _s.Length)
                {
                    return null;
                }

                var startIndex = _index;
                var endIndex = _s.IndexOf('\n', startIndex);

                if (endIndex == -1)
                {
                    // Consider this as an error: PeakToEnd() should be explicitly used
                    return null;
                }

                _index = endIndex + 1;

                return pool is not null
                    ? pool.Intern(_s, startIndex, endIndex - startIndex)
                    : _s.Substring(startIndex, endIndex - startIndex);
            }

            public StringLineReader TrimStart()
            {
                while (_index < _s.Length && char.IsWhiteSpace(_s[_index]))
                {
                    _index++;
                }

                return this;
            }

            private void TrimEnd(ref int endIndex)
            {
                while (endIndex > _index && char.IsWhiteSpace(_s[endIndex - 1]))
                {
                    endIndex--;
                }
            }

            public (string? subject, string? body, bool hasMultiLineMessage) ReadToEndOfBody(bool skipBody, bool hasPathFilter)
            {
                if (_index == _s.Length)
                {
                    return (null, null, false);
                }

                int endOfBodyIndex;
                int endBodyIndex;
                if (hasPathFilter)
                {
                    // This check requires considerable CPU resources
                    endBodyIndex = _s.LastIndexOf(EndOfBody, _s.Length - 1, _s.Length - _index);
                    if (endBodyIndex < 0)
                    {
                        return (null, null, false);
                    }

                    // Advance to after the marker
                    endOfBodyIndex = endBodyIndex + EndOfBody.Length;
                }
                else
                {
                    endBodyIndex = _s.Length;
                    endOfBodyIndex = endBodyIndex;
                }

                TrimEnd(ref endBodyIndex);

                int endSubjectIndex = _s.IndexOf('\n', _index, endBodyIndex - _index);
                if (endSubjectIndex < 0)
                {
                    endSubjectIndex = endBodyIndex;
                }
                else
                {
                    TrimEnd(ref endSubjectIndex);
                }

                // Subject can also be defined as the contents before empty line (%s for --pretty),
                // this uses the alternative definition of first line in body.
                string subject = _s.Substring(_index, endSubjectIndex - _index);
                bool hasMultiLineMessage = endSubjectIndex < endBodyIndex;

                // Skip body if identical to subject
                string? body = skipBody || !hasMultiLineMessage
                    ? null
                    : _s.Substring(_index, endBodyIndex - _index);

                _index = endOfBodyIndex;

                return (subject, body, hasMultiLineMessage);
            }

            /// <summary>
            /// Return the remaining string, without advancing _index
            /// </summary>
            /// <returns>Reminder string</returns>
            public string? PeakToEnd()
            {
                if (_index >= _s.Length)
                {
                    return null;
                }

                return _s.Substring(_index);
            }
        }

        #endregion

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly RevisionReader _revisionReader;

            internal TestAccessor(RevisionReader revisionReader)
            {
                _revisionReader = revisionReader;
            }

            internal ArgumentBuilder BuildArgumentsBuildArguments(RefFilterOptions refFilterOptions,
                string branchFilter, string revisionFilter, string pathFilter, bool hasPathFilter) =>
                _revisionReader.BuildArguments(refFilterOptions, branchFilter, revisionFilter, pathFilter, hasPathFilter);

            internal static string EndOfBody => RevisionReader.EndOfBody;
        }
    }
}
