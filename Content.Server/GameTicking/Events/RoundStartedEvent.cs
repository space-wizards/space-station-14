using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Events;

/// <summary>
///     Raised at the end of <see cref="GameTicker.StartRound"/>
/// </summary>
public class RoundStartedEvent : EntityEventArgs
{
    public RoundStartedEvent(int roundId)
    {
        RoundId = roundId;
    }

    public int RoundId { get; set; }
}
