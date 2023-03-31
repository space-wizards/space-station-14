using Robust.Server.Player;

namespace Content.Server.Afk.Events;

/// <summary>
/// Raised whenever a player is no longer AFK.
/// </summary>
[ByRefEvent]
public readonly struct UnAFKEvent
{
    public readonly IPlayerSession Session;

    public UnAFKEvent(IPlayerSession playerSession)
    {
        Session = playerSession;
    }
}
