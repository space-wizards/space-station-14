using Content.Shared.Mind;

namespace Content.Shared.Roles;

/// <summary>
///     Base event raised on player entities to indicate that something changed about one of their roles.
/// </summary>
/// <param name="MindId">The mind id associated with the player.</param>
/// <param name="Mind">The mind component associated with the mind id.</param>
/// <param name="Antagonist">Whether or not the role makes the player an antagonist.</param>
public abstract record RoleEvent(EntityUid MindId, MindComponent Mind, bool Antagonist);
