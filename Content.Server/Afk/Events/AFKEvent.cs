using Robust.Server.Player;

namespace Content.Server.Afk.Events;

/// <summary>
/// Raised whenever a player goes afk.
/// </summary>
[ByRefEvent]
public readonly struct AFKEvent
{
    public readonly IPlayerSession Session;

    public AFKEvent(IPlayerSession playerSession)
    {
        Session = playerSession;
    }
}
