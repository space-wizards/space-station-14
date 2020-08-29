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
        public readonly int InletConduitLayer;
        public readonly int OutletConduitLayer;

        public PumpVisualState(PipeDirection inletDirection, PipeDirection outletDirection, int inletConduitLayer, int outletConduitLayer)
        {
            InletDirection = inletDirection;
            OutletDirection = outletDirection;
            InletConduitLayer = inletConduitLayer;
            OutletConduitLayer = outletConduitLayer;
        }
    }
}
