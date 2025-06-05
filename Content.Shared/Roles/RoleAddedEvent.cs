using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Raised on mind entities when a mind role is added to them.
/// </summary>
/// <param name="MindId">The mind id associated with the player.</param>
/// <param name="Mind">The mind component associated with the mind id.</param>
/// <param name="RoleTypeUpdate">True if this update has changed the mind's role type</param>
/// <param name="Silent">If true, Job greeting/intro will not be sent to the player's chat</param>
public sealed record RoleAddedEvent(EntityUid MindId, MindComponent Mind, bool RoleTypeUpdate, bool Silent = false) : RoleEvent(MindId, Mind, RoleTypeUpdate);
