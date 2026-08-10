using Robust.Shared.Player;

namespace Content.Shared.Afk.Events;

/// <summary>
/// Raised whenever a player is no longer AFK.
/// </summary>
[ByRefEvent]
public readonly struct UnAfkEvent
{
    public readonly ICommonSession Session;

    public UnAfkEvent(ICommonSession session)
    {
        Session = session;
    }
}
