using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Raised on mind entities when a mind role is added to them.
/// </summary>
/// <param name="MindRoleEntity">The mind role entity which was added.</param>
/// <param name="MindRoleEntity">The mind entity associated with the player.</param>
/// <param name="RoleTypeUpdate">True if this update has changed the mind's role type</param>
/// <param name="Silent">If true, Job greeting/intro will not be sent to the player's chat</param>
public sealed record RoleAddedEvent(Entity<MindRoleComponent> MindRoleEntity, Entity<MindComponent> MindEntity, bool RoleTypeUpdate, bool Silent = false) : RoleEvent(MindEntity, RoleTypeUpdate);
