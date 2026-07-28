using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Afk.Events;

/// <summary>
/// Raised whenever a player goes afk.
/// </summary>
[Serializable, NetSerializable]
public sealed class AfkEvent : EntityEventArgs
{
    public readonly NetUserId UserId;

    public AfkEvent(NetUserId userId)
    {
        UserId = userId;
    }
}
