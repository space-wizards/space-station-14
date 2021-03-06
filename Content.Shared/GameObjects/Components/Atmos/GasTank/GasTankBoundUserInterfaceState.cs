#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos.GasTank
{
    [Serializable, NetSerializable]
    public class GasTankBoundUserInterfaceState : BoundUserInterfaceState
    {
        public float TankPressure { get; set; }
        public float? OutputPressure { get; set; }
        public bool InternalsConnected { get; set; }
        public bool CanConnectInternals { get; set; }

    }
}
