namespace Content.Server.Ghost.Roles.Events;

/// <summary>
///     Raise to acquire the total number of ghost roles available.
///     TODO: Method events bad?
/// </summary>
public sealed class GhostRoleCountRequestedMessage : EntityEventArgs
{
    public int Count = 0;
}
