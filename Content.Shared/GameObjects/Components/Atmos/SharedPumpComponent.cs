#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
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
        public readonly bool PumpEnabled;

        public PumpVisualState(PipeDirection inletDirection, PipeDirection outletDirection, bool pumpEnabled)
        {
            InletDirection = inletDirection;
            OutletDirection = outletDirection;
            PumpEnabled = pumpEnabled;
        }
    }
}
