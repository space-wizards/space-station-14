using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Content.Shared.Drone.Components;

namespace Content.Shared.Drone
{
    public abstract class SharedDroneSystem : EntitySystem
    {
         public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneToolComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        }

        private void OnRemoveAttempt(EntityUid uid, DroneToolComponent tool, ContainerGettingRemovedAttemptEvent args)
        {
            args.Cancel();
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
