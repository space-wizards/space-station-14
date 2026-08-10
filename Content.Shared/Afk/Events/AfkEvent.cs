using Robust.Shared.Player;

namespace Content.Shared.Afk.Events;

/// <summary>
/// Raised whenever a player goes afk.
/// </summary>
[ByRefEvent]
public readonly struct AfkEvent
{
    public readonly ICommonSession Session;

    public AfkEvent(ICommonSession session)
    {
        Session = session;
    }
}
