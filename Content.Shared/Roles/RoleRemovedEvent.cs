using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Event raised on player entities to indicate that a role was removed from their mind.
/// </summary>
/// <param name="MindId">The mind id associated with the player.</param>
/// <param name="Mind">The mind component associated with the mind id.</param>
/// <param name="Antagonist">
///     Whether or not the role made the player an antagonist.
///     They may still be one due to one of their other roles.
/// </param>
public sealed record RoleRemovedEvent(EntityUid MindId, MindComponent Mind, bool Antagonist) : RoleEvent(MindId, Mind, Antagonist);
