namespace Content.Shared.Roles;

/// <summary>
///     Raised on mind entities when a role is added to them.
///     <see cref="RoleAddedEvent"/> for the one raised on player entities.
/// </summary>
///
[Obsolete("Use RoleAddedEvent")]
[ByRefEvent]
public readonly record struct MindRoleAddedEvent(bool Silent);
