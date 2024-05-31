using Robust.Shared.Player;

namespace Content.Server.Afk.Events;

/// <summary>
/// Raised whenever a player goes afk.
/// </summary>
[ByRefEvent]
public readonly struct AFKEvent
{
    public readonly ICommonSession Session;

    public AFKEvent(ICommonSession playerSession)
    {
        Session = playerSession;
    }
}
