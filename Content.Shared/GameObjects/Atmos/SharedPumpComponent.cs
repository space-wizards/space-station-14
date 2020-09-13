using System;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Shared.Serialization;

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
        public readonly ConduitLayer InletConduitLayer;
        public readonly ConduitLayer OutletConduitLayer;
        public readonly bool PumpEnabled;

        public PumpVisualState(PipeDirection inletDirection, PipeDirection outletDirection, ConduitLayer inletConduitLayer, ConduitLayer outletConduitLayer, bool pumpEnabled)
        {
            InletDirection = inletDirection;
            OutletDirection = outletDirection;
            InletConduitLayer = inletConduitLayer;
            OutletConduitLayer = outletConduitLayer;
            PumpEnabled = pumpEnabled;
        }
    }
}
