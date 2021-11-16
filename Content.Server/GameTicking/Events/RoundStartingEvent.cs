using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Events;

/// <summary>
///     Raised at the start of <see cref="GameTicker.StartRound"/>
/// </summary>
public class RoundStartingEvent : EntityEventArgs
{
    public RoundStartingEvent(int roundId)
    {
        RoundId = roundId;
    }

    public int RoundId { get; set; }
}
