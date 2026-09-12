using Robust.Shared.Player;

namespace Content.Shared.Afk.Events;

/// <summary>
/// Raised whenever a player is no longer AFK.
/// </summary>
[ByRefEvent]
public readonly struct UnAfkEvent(ICommonSession session)
{
    public readonly ICommonSession Session = session;
}
