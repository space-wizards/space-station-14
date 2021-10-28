using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Devices
{
    [RegisterComponent]
    public class SharedProximitySensorComponent : Component
    {
        public override string Name => "ProximitySensor";

        public const int MaxRange = 10;
        public const int MinRange = 1;

        public const int MaxArmingTime = 5;
        public const int MinArmingTime = 0;

        public const string ProximityTriggerFixture = "prox_trigger";

        public int Range = 1;
        public bool IsActive = false;
        public bool IsArmed = false;
        public int ArmingTime = 3;

        public TimeSpan TimeActivated = TimeSpan.Zero;
        public TimeSpan TimeArmed = TimeSpan.Zero;
    }

    [Serializable, NetSerializable]
    public enum ProximitySensorUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents a <see cref="SharedProximitySensorComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class ProximitySensorBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int Range { get; }
        public bool IsActive { get; }
        public int ArmingTime { get; }

        public ProximitySensorBoundUserInterfaceState(int range, bool isActive, int armingTime)
        {
            Range = range;
            IsActive = isActive;
            ArmingTime = armingTime;
        }
    }

    [Serializable, NetSerializable]
    public class ProximitySensorUpdateSensorMessage : BoundUserInterfaceMessage
    {
        public int Range { get; }
        public int ArmingTime { get; }

        public ProximitySensorUpdateSensorMessage(int range, int time)
        {
            Range = range;
            ArmingTime = time;
        }
    }

    [Serializable, NetSerializable]
    public class ProximitySensorUpdateActiveMessage : BoundUserInterfaceMessage
    {
        public bool Active { get; }

        public ProximitySensorUpdateActiveMessage(bool active)
        {
            Active = active;
        }
    }
}
