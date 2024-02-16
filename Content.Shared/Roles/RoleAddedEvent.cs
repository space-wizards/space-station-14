using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Raised on player entities when a role is added to them.
///     <see cref="RoleAddedEvent"/> for the one raised on mind entities.
/// </summary>
/// <param name="MindId">The mind id associated with the player.</param>
/// <param name="Mind">The mind component associated with the mind id.</param>
/// <param name="Antagonist">Whether or not the role makes the player an antagonist.</param>
public sealed record RoleAddedEvent(EntityUid MindId, MindComponent Mind, bool Antagonist, bool Silent = false) : RoleEvent(MindId, Mind, Antagonist);
