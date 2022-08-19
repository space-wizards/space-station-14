using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Ghost.Roles.Events;

/// <summary>
/// Raised when a ghost role's availability changes. This could happen because:
/// <list type="bullet">
///   <item>The ghost role entity got taken over</item>
///   <item>The ghost role entity died</item>
///   <item>The ghost role was reserved by <see cref="GhostRoleGroupSystem"/></item>
///   <item>The ghost role component was removed</item>
/// </list>
/// </summary>
public sealed class GhostRoleAvailabilityChangedEvent : EntityEventArgs
{
    public readonly EntityUid GhostRoleEntity;

    public readonly GhostRoleComponent GhostRole;

    /// <summary>
    /// True if the ghost role became available. False if it became unavailable.
    /// </summary>
    public readonly bool Available;

    public GhostRoleAvailabilityChangedEvent(EntityUid entity, GhostRoleComponent component, bool available)
    {
        GhostRoleEntity = entity;
        GhostRole = component;
        Available = available;
    }
}
