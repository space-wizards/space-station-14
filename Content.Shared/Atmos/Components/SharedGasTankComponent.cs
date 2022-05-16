using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components
{
    [Serializable, NetSerializable]
    public enum SharedGasTankUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class GasTankToggleInternalsMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class GasTankSetPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class GasTankBoundUserInterfaceState : BoundUserInterfaceState
    {
        public float TankPressure { get; set; }
        public float? OutputPressure { get; set; }
        public bool InternalsConnected { get; set; }
        public bool CanConnectInternals { get; set; }

    }
}
