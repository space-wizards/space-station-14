using Content.Shared.CCVar;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.TypingIndicator;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.TypingIndicator;

public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    private static readonly TimeSpan TypingTimeout = TimeSpan.FromSeconds(2);
    private TimeSpan _lastTextChange;
    private bool _isEnabled;
    private bool _isTyping;
    private bool _isChatFocused;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TypingIndicatorComponent, GetStatusIconsEvent>(OnGetStatusIcon);

        _cfg.OnValueChanged(CCVars.ChatShowTypingIndicator, v => _isEnabled = v, true);
    }

    public void OnChangedChatText()
    {
        _isTyping = true;
        _lastTextChange = _time.CurTime;
        UpdateTypingStatus();
    }

    public void OnSubmittedChatText()
    {
        _isTyping = false;
        _isChatFocused = false;
        UpdateTypingStatus();
    }

    public void OnChangedChatFocus(bool isFocused)
    {
        _isChatFocused = isFocused;
    }

    private void OnGetStatusIcon(EntityUid uid, TypingIndicatorComponent component, ref GetStatusIconsEvent args)
    {
        if (!_prototype.TryIndex<TypingIndicatorPrototype>(component.Prototype, out var typingIndicatorProto))
            return;

        var typingIconProto = component.Status switch
        {
            TypingStatus.Typing => typingIndicatorProto.TypingIcon,
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(typingIconProto))
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(typingIconProto));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_isTyping)
        {
            var elapsed = _time.CurTime - _lastTextChange;
            if (elapsed > TypingTimeout)
            {
                _isTyping = false;
                UpdateTypingStatus();
            }
        }
    }

    private void UpdateTypingStatus()
    {
        if (!_isEnabled)
            return;

        var status = TypingStatus.None;
        if (_isChatFocused)
            status = _isTyping ? TypingStatus.Typing : TypingStatus.Idle;

        RaiseNetworkEvent(new TypingChangedEvent(status));
    }
}
