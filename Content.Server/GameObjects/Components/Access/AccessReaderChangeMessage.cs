using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Access
{
    public sealed class AccessReaderChangeMessage : EntitySystemMessage
    {
        public EntityUid Uid { get; }
        public bool Enabled { get; }

        public AccessReaderChangeMessage(EntityUid uid, bool enabled)
        {
            Uid = uid;
            Enabled = enabled;
        }
    }
}