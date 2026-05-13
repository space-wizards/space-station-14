namespace Content.Server.Access
{
    public sealed partial class AccessReaderChangeEvent : EntityEventArgs
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

