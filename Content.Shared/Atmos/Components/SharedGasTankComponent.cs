#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components
{
    [Serializable, NetSerializable]
    public enum SharedGasTankUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public class GasTankToggleInternalsMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public class GasTankSetPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; set; }
    }

    [Serializable, NetSerializable]
    public class GasTankBoundUserInterfaceState : BoundUserInterfaceState
    {
        public float TankPressure { get; set; }
        public float? OutputPressure { get; set; }
        public bool InternalsConnected { get; set; }
        public bool CanConnectInternals { get; set; }

    }
}
