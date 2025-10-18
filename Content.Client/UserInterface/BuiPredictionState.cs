using System.Linq;
using Robust.Client.Timing;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface;

/// <summary>
/// A local buffer for <see cref="BoundUserInterface"/>s to manually implement prediction.
/// </summary>
/// <remarks>
/// <para>
/// In many current (and future) cases, it is not practically possible to implement prediction for UIs
/// by implementing the logic in shared. At the same time, we want to implement prediction for the best user experience
/// (and it is sometimes the easiest way to make even a middling user experience).
/// </para>
/// <para>
/// You can queue predicted messages into this class with <see cref="SendMessage"/>,
/// and then call <see cref="MessagesToReplay"/> later from <see cref="BoundUserInterface.UpdateState"/>
/// to get all messages that are still "ahead" of the latest server state.
/// These messages can then manually be "applied" to the latest state received from the server.
/// </para>
/// <para>
/// Note that this system only works if the server is guaranteed to send some kind of update in response to UI messages,
/// or at a regular schedule. If it does not, there is no opportunity to error correct the prediction.
/// </para>
/// </remarks>
public sealed class BuiPredictionState
{
    private readonly BoundUserInterface _parent;
    private readonly IClientGameTiming _gameTiming;

    private readonly Queue<MessageData> _queuedMessages = new();

    public BuiPredictionState(BoundUserInterface parent, IClientGameTiming gameTiming)
    {
        _parent = parent;
        _gameTiming = gameTiming;
    }

    public void SendMessage(BoundUserInterfaceMessage message)
    {
        if (_gameTiming.IsFirstTimePredicted)
        {
            var messageData = new MessageData
            {
                TickSent = _gameTiming.CurTick,
                Message = message,
            };

            _queuedMessages.Enqueue(messageData);
        }

        _parent.SendPredictedMessage(message);
    }

    public IEnumerable<BoundUserInterfaceMessage> MessagesToReplay()
    {
        var curTick = _gameTiming.LastRealTick;
        while (_queuedMessages.TryPeek(out var data) && data.TickSent <= curTick)
        {
            _queuedMessages.Dequeue();
        }

        if (_queuedMessages.Count == 0)
            return [];

        return _queuedMessages.Select(c => c.Message);
    }

    private struct MessageData
    {
        public GameTick TickSent;
        public required BoundUserInterfaceMessage Message;

        public override string ToString()
        {
            return $"{Message} @ {TickSent}";
        }
    }
}
