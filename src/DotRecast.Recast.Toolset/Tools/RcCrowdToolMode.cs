﻿using DotRecast.Core;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcCrowdToolMode
    {
        public static readonly RcCrowdToolMode CREATE = new RcCrowdToolMode(0, "Create Agents");
        public static readonly RcCrowdToolMode MOVE_TARGET = new RcCrowdToolMode(1, "Move Target");
        public static readonly RcCrowdToolMode SELECT = new RcCrowdToolMode(2, "Select Agent");
        public static readonly RcCrowdToolMode TOGGLE_POLYS = new RcCrowdToolMode(3, "Toggle Polys");

        public static readonly RcImmutableArray<RcCrowdToolMode> Values = RcImmutableArray.Create(
            CREATE,
            MOVE_TARGET,
            SELECT,
            TOGGLE_POLYS
        );

        public int Idx { get; }
        public string Label { get; }

        private RcCrowdToolMode(int idx, string label)
        {
            Idx = idx;
            Label = label;
        }
    }
}