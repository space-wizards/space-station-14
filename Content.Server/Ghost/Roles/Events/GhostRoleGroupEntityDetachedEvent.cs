namespace Content.Server.Ghost.Roles.Events;

/// <summary>
/// Raised on an entity after it is detached from a ghost role group.
/// </summary>
public sealed class GhostRoleGroupEntityDetachedEvent : EntityEventArgs
{
    /// <summary>
    /// The entity that was detached from a role group.
    /// </summary>
    public EntityUid Attached;

    /// <summary>
    /// The identifier of the role group.
    /// </summary>
    public uint RoleGroup;

    public GhostRoleGroupEntityDetachedEvent(EntityUid attached, uint roleGroup)
    {
        Attached = attached;
        RoleGroup = roleGroup;
    }
}
