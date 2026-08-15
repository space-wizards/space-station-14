using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

[Serializable, NetSerializable]
public sealed class ClickMessageSenderRequestEvent(NetEntity sender) : EntityEventArgs
{
    public readonly NetEntity Sender = sender;
}
