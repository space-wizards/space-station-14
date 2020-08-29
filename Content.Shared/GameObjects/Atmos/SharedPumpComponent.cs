using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Atmos
{
    [Serializable, NetSerializable]
    public enum PumpVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class PumpVisualState
    {
        public readonly PipeDirection InletDirection;
        public readonly PipeDirection OutletDirection;

        public PumpVisualState(PipeDirection inletDirection, PipeDirection outletDirection)
        {
            InletDirection = inletDirection;
            OutletDirection = outletDirection;
        }
    }
}
