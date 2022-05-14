namespace Content.Server.Actions.Events
{
    public sealed class DisarmAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid TargetUid;
        public readonly EntityUid DisarmerUid;
        public readonly EntityUid? TargetItemInHandUid;

        public DisarmAttemptEvent(EntityUid targetUid, EntityUid disarmerUid, EntityUid? targetItemInHandUid = null)
        {
            TargetUid = targetUid;
            DisarmerUid = disarmerUid;
            TargetItemInHandUid = targetItemInHandUid;
        }
    }
}
