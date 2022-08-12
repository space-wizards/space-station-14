namespace Content.Server.Ghost.Roles.Events;

/// <summary>
///     Raise to acquire the total number of ghost roles available.
/// </summary>
public sealed class GhostRoleCountRequestedEvent : EntityEventArgs
{
    public int Count = 0;
}
