using Content.Shared.Chat.TypingIndicator;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Chat.TypingIndicator;

public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly TimeSpan _typingTimeout = TimeSpan.FromSeconds(2);
    private TimeSpan _lastTextChange;
    private bool _isClientTyping;

    public void ClientChangedChatText()
    {
        // client typed something - show typing indicator
        ClientUpdateTyping(true);
        _lastTextChange = _time.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check if client didn't changed chat text box for a long time
        if (_isClientTyping)
        {
            var dif = _time.CurTime - _lastTextChange;
            if (dif > _typingTimeout)
            {
                // client didn't typed anything for a long time - hide indicator
                ClientUpdateTyping(false);
            }
        }
    }

    private void ClientUpdateTyping(bool isClientTyping)
    {
        if (_isClientTyping == isClientTyping)
            return;
        _isClientTyping = isClientTyping;

        // check if player controls any pawn
        var playerPawn = _playerManager.LocalPlayer?.ControlledEntity;
        if (playerPawn == null)
            return;

        // send a networked event to player
        RaiseNetworkEvent(new TypingChangedEvent(playerPawn.Value, isClientTyping));
    }
}
