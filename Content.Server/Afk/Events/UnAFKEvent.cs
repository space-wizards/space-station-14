using Robust.Shared.Player;

namespace Content.Server.Afk.Events;

/// <summary>
/// Raised whenever a player is no longer AFK.
/// </summary>
[ByRefEvent]
public readonly struct UnAFKEvent
{
    public readonly ICommonSession Session;

    public UnAFKEvent(ICommonSession playerSession)
    {
        Session = playerSession;
    }
}
