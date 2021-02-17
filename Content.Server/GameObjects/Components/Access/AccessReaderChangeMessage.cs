using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Access
{
    public sealed class AccessReaderChangeMessage : EntitySystemMessage
    {
        public IEntity Sender { get; }

        public bool Enabled { get; }

        public AccessReaderChangeMessage(IEntity entity, bool enabled)
        {
            Sender = entity;
            Enabled = enabled;
        }
    }
}