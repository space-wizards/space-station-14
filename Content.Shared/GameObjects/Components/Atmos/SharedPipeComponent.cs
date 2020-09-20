using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [Serializable, NetSerializable]
    public enum PipeVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class PipeVisualStateSet
    {
        public readonly PipeVisualState[] PipeVisualStates;

        public PipeVisualStateSet(PipeVisualState[] pipeVisualStates)
        {
            PipeVisualStates = pipeVisualStates;
        }
    }

    [Serializable, NetSerializable]
    public class PipeVisualState
    {
        public readonly PipeDirection PipeDirection;
        public readonly ConduitLayer ConduitLayer;

        public PipeVisualState(PipeDirection pipeDirection, ConduitLayer conduitLayer)
        {
            PipeDirection = pipeDirection;
            ConduitLayer = conduitLayer;
        }
    }

    public enum PipeDirection
    {
        None = 0,

        //Half of a pipe in a direction
        North = 1 << 0,
        South = 1 << 1,
        West = 1 << 2,
        East = 1 << 3,

        //Straight pipes
        Longitudinal = North | South,
        Lateral = West | East,

        //Bends
        NWBend = North | West,
        NEBend = North | East,
        SWBend = South | West,
        SEBend = South | East,

        //T-Junctions
        TNorth = North | Lateral,
        TSouth = South | Lateral,
        TWest = West | Longitudinal,
        TEast = East | Longitudinal,

        //Four way
        Fourway = North | South | East | West,

        All = -1,
    }

    public enum ConduitLayer
    {
        One = 1,
        Two = 2,
        Three = 3,
    }
}
