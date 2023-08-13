using Content.Client.Hands;
using Content.Client.Options.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Gameplay
{
    [Virtual]
    public class GameplayState : GameplayStateBase, IMainViewportState
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private FpsCounter _fpsCounter = default!;

        public MainViewport Viewport => _uiManager.ActiveScreen!.GetWidget<MainViewport>()!;

        private readonly GameplayStateLoadController _loadController;

        public GameplayState()
        {
            IoCManager.InjectDependencies(this);

            _loadController = _uiManager.GetUIController<GameplayStateLoadController>();
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
            _configurationManager.OnValueChanged(CCVars.UIOpacity, UpdateUIOpacity);
            _uiManager.WindowRoot.OnChildAdded += (child) => UpdateChildUIOpacity(child, _configurationManager.GetCVar(CCVars.UIOpacity));
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

        private void UpdateChildUIOpacity(Control child, float value)
        {
            if (child is OptionsMenu || child is EscapeMenu)
                return;
            child.Modulate = child.Modulate.WithAlpha(value / 100f);
        }

        private void UpdateUIOpacity(float value)
        {
            foreach (var child in _uiManager.WindowRoot.Children)
            {
                UpdateChildUIOpacity(child, value);
            }
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
            _loadController.UnloadScreen();
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

            _loadController.LoadScreen();
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
