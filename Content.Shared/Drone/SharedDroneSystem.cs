using Robust.Shared.Serialization;

namespace Content.Shared.Drone
{
    public abstract class SharedDroneSystem : EntitySystem
    {
        [Serializable, NetSerializable]
        public enum DroneVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum DroneStatus : byte
        {
            Off,
            On
        }
    }

    [Serializable, NetSerializable]
    public sealed class DroneBuiState : BoundUserInterfaceState
    {
        public float ChargePercent;

        public bool HasBattery;

        public DroneBuiState(float chargePercent, bool hasBattery)
        {
            ChargePercent = chargePercent;
            HasBattery = hasBattery;
        }
    }

    [Serializable, NetSerializable]
    public enum DroneUiKey : byte
    {
        Key
    }
}
