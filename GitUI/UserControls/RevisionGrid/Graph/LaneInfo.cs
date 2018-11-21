using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GitCommands;
using GitUIPluginInterfaces;

namespace GitUI.UserControls.RevisionGrid.Graph
{
    public class LaneInfo
    {
        private static Random random = new Random();

        public LaneInfo(RevisionGraphRevision startRevision)
        {
            Color = startRevision.Objectid.GetHashCode();
            StartRevision = startRevision;
        }

        public RevisionGraphRevision StartRevision { get; set; }

        public bool IsMergeLane { get; set; }

        public int StartScore
        {
            get
            {
                return StartRevision.Score;
            }
        }

        public int Color { get; private set; }
    }
}
