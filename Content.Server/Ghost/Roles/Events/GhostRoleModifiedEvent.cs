using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Ghost.Roles.Events;

/// <summary>
/// Raised when the ghost role data fields are modified. Fields that have null values
/// are unmodified, otherwise they will contain previous values.
/// </summary>
public sealed class GhostRoleModifiedEvent : EntityEventArgs
{
    public readonly GhostRoleComponent GhostRole;

    public string? PreviousRoleName { get; init; }
    public string? PreviousRoleRule { get; init; }
    public string? PreviousRoleDescription { get; init; }
    public bool? PreviousRoleLotteryEnabled { get; init; }

    public GhostRoleModifiedEvent(GhostRoleComponent ghostRole)
    {
        GhostRole = ghostRole;
    }
}
