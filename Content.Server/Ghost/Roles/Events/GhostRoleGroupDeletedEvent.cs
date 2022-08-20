namespace Content.Server.Ghost.Roles.Events;

public sealed class GhostRoleGroupDeletedEvent : EntityEventArgs
{
    public readonly uint RoleGroupIdentifier;

    public GhostRoleGroupDeletedEvent(uint identifier)
    {
        RoleGroupIdentifier = identifier;
    }
}
