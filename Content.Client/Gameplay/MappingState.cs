using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Viewport;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Gameplay;

public sealed class MappingState : GameplayStateBase, IMainViewportState
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IInputManager _input = default!;

    private FpsCounter _fpsCounter = default!;

    private readonly ViewportUIController _viewportController;

    public MainViewport Viewport => _uiManager.ActiveScreen!.GetWidget<MainViewport>()!;

    public MappingState()
    {
        IoCManager.InjectDependencies(this);
        _viewportController = _uiManager.GetUIController<ViewportUIController>();
    }

    protected override void Startup()
    {
        base.Startup();

        LoadMainScreen();
        // FPS counter.
        // yeah this can just stay here, whatever
        // look ma COPY PASTE! --future coder
        _fpsCounter = new FpsCounter(_gameTiming);
        UserInterfaceManager.PopupRoot.AddChild(_fpsCounter);
        _fpsCounter.Visible = _configurationManager.GetCVar(CCVars.HudFpsCounterVisible);
        _configurationManager.OnValueChanged(CCVars.HudFpsCounterVisible, (show) => { _fpsCounter.Visible = show; });
        _configurationManager.OnValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
        _input.Contexts.SetActiveContext("mapping");
    }

    protected override void Shutdown()
    {
        base.Shutdown();
        // Clear viewport to some fallback, whatever.
        _eyeManager.MainViewport = UserInterfaceManager.MainViewport;
        _fpsCounter.Dispose();
        _uiManager.ClearWindows();
        _configurationManager.UnsubValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
        UnloadMainScreen();
        _input.Contexts.SetActiveContext("human");
    }

    private void ReloadMainScreenValueChange(string _)
    {
        ReloadMainScreen();
    }

    public void ReloadMainScreen()
    {
        if (_uiManager.ActiveScreen?.GetWidget<MainViewport>() == null)
        {
            return;
        }

        UnloadMainScreen();
        LoadMainScreen();
    }

    private void UnloadMainScreen()
    {
        _uiManager.UnloadScreen();
    }

    private void LoadMainScreen()
    {
        _uiManager.LoadScreen<MappingGameScreen>();
        _viewportController.ReloadViewport();
    }
}
