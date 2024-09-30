using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Content.Shared._Harmony.CCVars;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;

namespace Content.Client._Harmony.RoundEnd;

[UsedImplicitly]
public sealed class RoundEndNoEorgUIController : UIController
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private RoundEndNoEorgWindow? _window;
    private float _timer;
    private bool _skipPopup;

    private bool GetSkipPopupCvar() => _cfg.GetCVar(HCCVars.SkipRoundEndNoEorgMessage);

    private void SetSkipPopupCvar(bool value)
    {
        if (GetSkipPopupCvar() == value)
            return;
        _cfg.SetCVar(HCCVars.SkipRoundEndNoEorgMessage, value);
        _cfg.SaveToFile();
    }

    public override void Initialize()
    {
        base.Initialize();
        if (GetSkipPopupCvar())
            return;
        SubscribeNetworkEvent<RoundEndMessageEvent>((_, _) => OpenRoundEndNoEorgWindow());
    }

    public void OpenRoundEndNoEorgWindow()
    {
        if (GetSkipPopupCvar())
            return;

        if (_window == null)
            InitializeWindow();

        _window?.MoveToFront();
    }

    private void InitializeWindow()
    {
        _window = new RoundEndNoEorgWindow();
        _timer = 5;
        _window.TimedCloseButton.OnPressed += OnClosePressed;
        _window.CheckBox.Pressed = GetSkipPopupCvar();
        _window.CheckBox.OnToggled += args => _skipPopup = args.Pressed;
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }

    public void OnClosePressed(BaseButton.ButtonEventArgs args)
    {
        SetSkipPopupCvar(_skipPopup);
        _window?.Close();
        _window = null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_window == null)
            return;

        if (!_window.TimedCloseButton.Disabled)
            return;

        if (_timer > 0.0f)
        {
            _timer -= args.DeltaSeconds;
            if (_timer < 0)
                _timer = 0;
        }

        _window.UpdateCloseButton(_timer);
    }
}
