using Robust.Shared.Serialization;

namespace Content.Shared.Roles;

/// <summary>
/// Sent from server to client to notify it of its role timers.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoleTimersEvent : EntityEventArgs
{
    public TimeSpan Overall;
    public Dictionary<string, TimeSpan> RoleTimes = new();
}
