using Robust.Shared.GameObjects;

namespace Content.Server.Actions.Events
{
    public class DisarmAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid TargetUid;
        public readonly EntityUid DisarmerUid;
        public DisarmAttemptEvent(EntityUid targetUid, EntityUid disarmerUid)
        {
            TargetUid = targetUid;
            DisarmerUid = disarmerUid;
        }
    }
}
