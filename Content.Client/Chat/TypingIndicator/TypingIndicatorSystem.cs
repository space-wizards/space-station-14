using Content.Shared.CCVar;
using Content.Shared.Chat.TypingIndicator;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Chat.TypingIndicator;

// Client-side typing system tracks user input in chat box
public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly TimeSpan _typingTimeout = TimeSpan.FromSeconds(2);
    private TimeSpan _lastTextChange;
    private bool _isClientTyping;
    private bool _isClientChatFocused;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.ChatShowTypingIndicator, OnShowTypingChanged);
    }

    public void ClientChangedChatText()
    {
        // don't update it if player don't want to show typing indicator
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        // client typed something - show typing indicator
        _isClientTyping = true;
        ClientUpdateTyping();
        _lastTextChange = _time.CurTime;
    }

    public void ClientSubmittedChatText()
    {
        // don't update it if player don't want to show typing
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        // client submitted text - hide typing indicator
        _isClientTyping = false;
        ClientUpdateTyping();
    }

    public void ClientChangedChatFocus(bool isFocused)
    {
        // don't update it if player don't want to show typing
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        // client submitted text - hide typing indicator
        _isClientChatFocused = isFocused;
        ClientUpdateTyping();
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
                // client didn't typed anything for a long time - change indicator
                _isClientTyping = false;
                ClientUpdateTyping();
            }
        }
    }

    private void ClientUpdateTyping()
    {
        // check if player controls any pawn
        if (_playerManager.LocalEntity == null)
            return;

        var state = TypingIndicatorState.None;
        if (_isClientChatFocused)
            state = _isClientTyping ? TypingIndicatorState.Typing : TypingIndicatorState.Idle;

        // send a networked event to server
        RaisePredictiveEvent(new TypingChangedEvent(state));
    }

    private void OnShowTypingChanged(bool showTyping)
    {
        // hide typing indicator immediately if player don't want to show it anymore
        if (!showTyping)
        {
            _isClientTyping = false;
            ClientUpdateTyping();
        }
    }
}
