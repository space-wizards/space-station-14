namespace Content.Server.Access
{
    public sealed class AccessReaderChangeEvent : EntityEventArgs
    {
        public EntityUid Sender { get; }

        public bool Enabled { get; }

        public AccessReaderChangeEvent(EntityUid entity, bool enabled)
        {
            Sender = entity;
            Enabled = enabled;
        }
    }
}
