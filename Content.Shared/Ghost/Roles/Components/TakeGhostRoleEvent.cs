using Robust.Shared.Player;

namespace Content.Shared.Ghost.Roles.Components;

/// <summary>
/// Raised when a player attempts to take a ghost role.
/// </summary>
[ByRefEvent]
public record struct TakeGhostRoleEvent(ICommonSession Player)
{
    /// <summary>
    /// Whether the event handler successfully granted the role.
    /// </summary>
    public bool TookRole { get; set; }
}
