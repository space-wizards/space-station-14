using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[NetSerializable, Serializable]
public sealed class PlayerInfoChangedEvent : EntityEventArgs
{
    public PlayerInfo? PlayerInfo;
}
