namespace Content.Server.Ghost.Roles.Events;


/// <summary>
/// Raised on an entity after it is attached to ghost role group.
/// </summary>
public class GhostRoleGroupEntityAttachedEvent
{
    /// <summary>
    /// The entity that was attached to a role group.
    /// </summary>
    public EntityUid Attached;

    /// <summary>
    /// The identifier of the role group.
    /// </summary>
    public uint RoleGroup;

    public GhostRoleGroupEntityAttachedEvent(EntityUid attached, uint roleGroup)
    {
        Attached = attached;
        RoleGroup = roleGroup;
    }
}
