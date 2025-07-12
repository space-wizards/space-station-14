using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Raised on mind entities when a mind role is removed from them.
/// </summary>
/// <param name="MindEntity">The mind entity associated with the player.</param>
/// <param name="RoleTypeUpdate">True if this update has changed the mind's role type</param>
public sealed record RoleRemovedEvent(Entity<MindComponent> MindEntity, bool RoleTypeUpdate) : RoleEvent(MindEntity, RoleTypeUpdate);
