using Content.Client.Alerts.UI;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.Chat.UI;
using Content.Client.Construction.UI;
using Content.Client.Hands;
using Content.Client.HUD;
using Content.Client.HUD.UI;
using Content.Client.Voting;
using Content.Shared.Chat;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Viewport
{
    public class GameScreen : GameScreenBase, IMainViewportState
    {
        public static readonly Vector2i ViewportSize = (EyeManager.PixelsPerMeter * 21, EyeManager.PixelsPerMeter * 15);

        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        [ViewVariables] private ChatBox? _gameChat;
        private ConstructionMenuPresenter? _constructionMenu;
        private AlertsFramePresenter? _alertsFramePresenter;

        private FpsCounter _fpsCounter = default!;

        public MainViewport Viewport { get; private set; } = default!;

        public override void Startup()
        {
            base.Startup();

            _gameChat = new HudChatBox {PreferredChannel = ChatSelectChannel.Local};

            UserInterfaceManager.StateRoot.AddChild(_gameChat);
            LayoutContainer.SetAnchorAndMarginPreset(_gameChat, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetAnchorAndMarginPreset(_gameChat, LayoutContainer.LayoutPreset.TopRight, margin: 10);
            LayoutContainer.SetMarginLeft(_gameChat, -475);
            LayoutContainer.SetMarginBottom(_gameChat, HudChatBox.InitialChatBottom);

            _chatManager.ChatBoxOnResized(new ChatResizedEventArgs(HudChatBox.InitialChatBottom));

            Viewport = new MainViewport
            {
                Viewport =
                {
                    ViewportSize = ViewportSize
                }
            };

            _userInterfaceManager.StateRoot.AddChild(Viewport);
            LayoutContainer.SetAnchorPreset(Viewport, LayoutContainer.LayoutPreset.Wide);
            Viewport.SetPositionFirst();

            _userInterfaceManager.StateRoot.AddChild(_gameHud.RootControl);
            _chatManager.SetChatBox(_gameChat);
            _voteManager.SetPopupContainer(_gameHud.VoteContainer);

            ChatInput.SetupChatInputHandlers(_inputManager, _gameChat);

            SetupPresenters();

            _eyeManager.MainViewport = Viewport.Viewport;

            _overlayManager.AddOverlay(new ShowHandItemOverlay());

            _fpsCounter = new FpsCounter(_gameTiming);
            _userInterfaceManager.StateRoot.AddChild(_fpsCounter);
            _fpsCounter.Visible = _configurationManager.GetCVar(CCVars.HudFpsCounterVisible);
            _configurationManager.OnValueChanged(CCVars.HudFpsCounterVisible, (show) => { _fpsCounter.Visible = show; });
        }

        public override void Shutdown()
        {
            _overlayManager.RemoveOverlay<ShowHandItemOverlay>();
            DisposePresenters();

            base.Shutdown();

            _gameChat?.Dispose();
            Viewport.Dispose();
            _gameHud.RootControl.Orphan();
            // Clear viewport to some fallback, whatever.
            _eyeManager.MainViewport = _userInterfaceManager.MainViewport;
            _fpsCounter.Dispose();
        }

        /// <summary>
        /// All UI Presenters should be constructed in here.
        /// </summary>
        private void SetupPresenters()
        {
            // HUD
            _alertsFramePresenter = new AlertsFramePresenter();

            // Windows
            _constructionMenu = new ConstructionMenuPresenter(_gameHud);
        }

        /// <summary>
        /// All UI Presenters should be disposed in here.
        /// </summary>
        private void DisposePresenters()
        {
            // Windows
            _constructionMenu?.Dispose();

            // HUD
            _alertsFramePresenter?.Dispose();
        }

        internal static void FocusChat(ChatBox chat)
        {
            if (chat.UserInterfaceManager.KeyboardFocused != null)
                return;

            chat.Focus();
        }

        internal static void FocusChannel(ChatBox chat, ChatSelectChannel channel)
        {
            if (chat.UserInterfaceManager.KeyboardFocused != null)
                return;

            chat.Focus(channel);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            Viewport.Viewport.Eye = _eyeManager.CurrentEye;
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
