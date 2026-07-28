using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Afk.Events;

/// <summary>
/// Raised whenever a player is no longer AFK.
/// </summary>
[Serializable, NetSerializable]
public sealed class UnAfkEvent : EntityEventArgs
{
    public readonly NetUserId UserId;

    public UnAfkEvent(NetUserId userId)
    {
        UserId = userId;
    }
}
