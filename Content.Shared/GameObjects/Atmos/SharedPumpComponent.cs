using Content.Shared.GameObjects.Components.Atmos;
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
        public readonly ConduitLayer InletConduitLayer;
        public readonly ConduitLayer OutletConduitLayer;

        public PumpVisualState(PipeDirection inletDirection, PipeDirection outletDirection, ConduitLayer inletConduitLayer, ConduitLayer outletConduitLayer)
        {
            InletDirection = inletDirection;
            OutletDirection = outletDirection;
            InletConduitLayer = inletConduitLayer;
            OutletConduitLayer = outletConduitLayer;
        }
    }
}
