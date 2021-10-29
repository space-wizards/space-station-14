using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Devices
{
    [RegisterComponent]
    public class SharedHealthSensorComponent : Component
    {
        public override string Name => "HealthSensor";

        public bool IsActive = false;

        public SensorMode Mode = SensorMode.Crit;

        public enum SensorMode
        {
            Crit,
            Death
        }
    }

    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum HealthSensorUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents a <see cref="SharedSignalerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class HealthSensorBoundUserInterfaceState : BoundUserInterfaceState
    {
        public bool IsActive { get; }
        public int Mode { get; }

        public HealthSensorBoundUserInterfaceState(bool active, int mode)
        {
            IsActive = active;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public class HealthSensorResetMessage : BoundUserInterfaceMessage
    {
        public HealthSensorResetMessage() {}
    }

    [Serializable, NetSerializable]
    public class HealthSensorUpdateModeMessage : BoundUserInterfaceMessage
    {
        public int Mode { get; }

        public HealthSensorUpdateModeMessage(int mode)
        {
            Mode = mode;
        }
    }
}
