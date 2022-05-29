using Robust.Shared.Serialization;

namespace Content.Shared.Drone
{
    public abstract class SharedDroneSystem : EntitySystem
    {
         public override void Initialize()
        {
            base.Initialize();
        }

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
}
