using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[NetSerializable, Serializable]
public sealed partial class PlayerInfoChangedEvent : EntityEventArgs
{
    public PlayerInfo? PlayerInfo;
}

