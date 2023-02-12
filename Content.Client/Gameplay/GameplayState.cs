using Content.Client.Construction.UI;
using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Alerts;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Ghost;
using Content.Client.UserInterface.Systems.Hands;
using Content.Client.UserInterface.Systems.Hotbar;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.MenuBar;
using Content.Client.UserInterface.Systems.Viewport;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Client.Player;
using Robust.Client.GameObjects;

namespace Content.Client.Gameplay
{
    public sealed class GameplayState : GameplayStateBase, IMainViewportState
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private FpsCounter _fpsCounter = default!;

        public MainViewport Viewport => _uiManager.ActiveScreen!.GetWidget<MainViewport>()!;

        private readonly GhostUIController _ghostController;
        private readonly ActionUIController _actionController;
        private readonly AlertsUIController _alertsController;
        private readonly HotbarUIController _hotbarController;
        private readonly ChatUIController _chatController;
        private readonly ViewportUIController _viewportController;
        private readonly GameTopMenuBarUIController _menuController;

        public GameplayState()
        {
            IoCManager.InjectDependencies(this);

            _ghostController = _uiManager.GetUIController<GhostUIController>();
            _actionController = _uiManager.GetUIController<ActionUIController>();
            _alertsController = _uiManager.GetUIController<AlertsUIController>();
            _hotbarController = _uiManager.GetUIController<HotbarUIController>();
            _chatController = _uiManager.GetUIController<ChatUIController>();
            _viewportController = _uiManager.GetUIController<ViewportUIController>();
            _menuController = _uiManager.GetUIController<GameTopMenuBarUIController>();
        }

        protected override void Startup()
        {
            base.Startup();

            LoadMainScreen();

            // Add the hand-item overlay.
            _overlayManager.AddOverlay(new ShowHandItemOverlay());

            // FPS counter.
            // yeah this can just stay here, whatever
            _fpsCounter = new FpsCounter(_gameTiming);
            UserInterfaceManager.PopupRoot.AddChild(_fpsCounter);
            _fpsCounter.Visible = _configurationManager.GetCVar(CCVars.HudFpsCounterVisible);
            _configurationManager.OnValueChanged(CCVars.HudFpsCounterVisible, (show) => { _fpsCounter.Visible = show; });
            _configurationManager.OnValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
        }

        protected override void Shutdown()
        {
            _overlayManager.RemoveOverlay<ShowHandItemOverlay>();

            base.Shutdown();
            // Clear viewport to some fallback, whatever.
            _eyeManager.MainViewport = UserInterfaceManager.MainViewport;
            _fpsCounter.Dispose();
            _uiManager.ClearWindows();
            _configurationManager.UnsubValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
            UnloadMainScreen();
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
            _chatController.SetMainChat(false);
            _menuController.UnloadButtons();
            _ghostController.UnloadGui();
            _actionController.UnloadGui();
            _uiManager.UnloadScreen();
        }

        private void LoadMainScreen()
        {
            var screenTypeString = _configurationManager.GetCVar(CCVars.UILayout);
            if (!Enum.TryParse(screenTypeString, out ScreenType screenType))
            {
                screenType = default;
            }

            switch (screenType)
            {
                case ScreenType.Default:
                    _uiManager.LoadScreen<DefaultGameScreen>();
                    break;
                case ScreenType.Separated:
                    _uiManager.LoadScreen<SeparatedChatGameScreen>();
                    break;
            }

            _chatController.SetMainChat(true);
            _viewportController.ReloadViewport();
            _menuController.LoadButtons();

            // TODO: This could just be like, the equivalent of an event or something
            _ghostController.LoadGui();
            _actionController.LoadGui();
            _alertsController.SyncAlerts();
            _hotbarController.ReloadHotbar();

            var viewportContainer = _uiManager.ActiveScreen!.FindControl<LayoutContainer>("ViewportContainer");
            _chatController.SetSpeechBubbleRoot(viewportContainer);
        }



        protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
        {
            if (args.Viewport == null)
                base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
            else
                base.OnKeyBindStateChanged(args);
        }
    }
}
