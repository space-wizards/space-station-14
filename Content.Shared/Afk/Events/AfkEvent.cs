using Robust.Shared.Player;

namespace Content.Shared.Afk.Events;

/// <summary>
/// Raised whenever a player goes afk.
/// </summary>
[ByRefEvent]
public readonly struct AfkEvent(ICommonSession session)
{
    public readonly ICommonSession Session = session;
}
