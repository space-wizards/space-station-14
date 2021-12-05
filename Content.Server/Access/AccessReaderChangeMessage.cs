using Robust.Shared.GameObjects;

namespace Content.Server.Access
{
    public sealed class AccessReaderChangeMessage : EntityEventArgs
    {
        public EntityUid Sender { get; }

        public bool Enabled { get; }

        public AccessReaderChangeMessage(EntityUid entity, bool enabled)
        {
            Sender = entity;
            Enabled = enabled;
        }
    }
}
