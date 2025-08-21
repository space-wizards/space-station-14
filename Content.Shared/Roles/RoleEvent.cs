using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Base event raised on mind entities to indicate that a mind role was either added or removed.
/// </summary>
/// <param name="MindId">The mind entity associated with the player.</param>
/// <param name="RoleTypeUpdate">True if this update has changed the mind's role type</param>
public abstract record RoleEvent(Entity<MindComponent> MindEntity, bool RoleTypeUpdate);
