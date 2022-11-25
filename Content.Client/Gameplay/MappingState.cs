using Content.Client.Hands;
using Content.Client.Mapping.Overlays;
using Content.Client.Parallax;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Viewport;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Mapping;
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
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IEntityNetworkManager _entNet = default!;
    [Dependency] private readonly IInputManager _input = default!;

    public const string MappingParallax = "MappingSDMM";

    private FpsCounter _fpsCounter = default!;

    private readonly ViewportUIController _viewportController;
    private readonly ParallaxSystem _parallaxSystem;

    public MainViewport Viewport => _uiManager.ActiveScreen!.GetWidget<MainViewport>()!;

    public MappingState()
    {
        IoCManager.InjectDependencies(this);
        _viewportController = _uiManager.GetUIController<ViewportUIController>();
        _parallaxSystem = _entSys.GetEntitySystem<ParallaxSystem>();
    }

    protected override void Startup()
    {
        base.Startup();

        LoadMainScreen();

        _overlayManager.AddOverlay(new MappingActivityOverlay());
        _overlayManager.AddOverlay(new ShowHandItemOverlay());
        // FPS counter.
        // yeah this can just stay here, whatever
        // look ma COPY PASTE! --future coder
        _fpsCounter = new FpsCounter(_gameTiming);
        UserInterfaceManager.PopupRoot.AddChild(_fpsCounter);
        _fpsCounter.Visible = _configurationManager.GetCVar(CCVars.HudFpsCounterVisible);
        _configurationManager.OnValueChanged(CCVars.HudFpsCounterVisible, (show) => { _fpsCounter.Visible = show; });
        _configurationManager.OnValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
        _entNet.SendSystemNetworkMessage(new EnterMappingModeEvent());
        _entMan.EventBus.RaiseEvent(EventSource.Local, new EnterMappingModeEvent());
        _parallaxSystem.Override = MappingParallax;
    }

    protected override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<MappingActivityOverlay>();
        _overlayManager.RemoveOverlay<ShowHandItemOverlay>();
        // Clear viewport to some fallback, whatever.
        _eyeManager.MainViewport = UserInterfaceManager.MainViewport;
        _fpsCounter.Dispose();
        _uiManager.ClearWindows();
        _configurationManager.UnsubValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
        UnloadMainScreen();
        _entNet.SendSystemNetworkMessage(new ExitMappingModeEvent());
        _entMan.EventBus.RaiseEvent(EventSource.Local, new ExitMappingModeEvent());
        _parallaxSystem.Override = null;
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
