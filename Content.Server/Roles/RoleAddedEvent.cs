using Content.Server.Mind;

namespace Content.Server.Roles;

/// <summary>
///     Event raised on player entities to indicate that a role was added to their mind.
/// </summary>
/// <param name="MindId">The mind id associated with the player.</param>
/// <param name="Mind">The mind component associated with the mind id.</param>
/// <param name="Antagonist">Whether or not the role makes the player an antagonist.</param>
public sealed record RoleAddedEvent(EntityUid MindId, MindComponent Mind, bool Antagonist) : RoleEvent(MindId, Mind, Antagonist);
