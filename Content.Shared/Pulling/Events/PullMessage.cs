namespace Content.Shared.Physics.Pull
{
    public abstract class PullMessage(EntityUid puller, EntityUid pulled) : EntityEventArgs
    {
        public readonly EntityUid Puller = puller;
        public readonly EntityUid Pulled = pulled;
    }
}
