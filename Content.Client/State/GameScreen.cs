using System.Linq;
using Content.Client.Administration;
using Content.Client.Chat;
using Content.Client.Construction;
using Content.Client.Interfaces.Chat;
using Content.Client.UserInterface;
using Content.Client.Voting;
using Content.Shared;
using Content.Shared.Chat;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.State
{
    public class GameScreen : GameScreenBase, IMainViewportState
    {
        public static readonly Vector2i ViewportSize = (EyeManager.PixelsPerMeter * 21, EyeManager.PixelsPerMeter * 15);

        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        [ViewVariables] private ChatBox? _gameChat;
        private ConstructionMenuPresenter? _constructionMenu;

        public MainViewport Viewport { get; private set; } = default!;

        public override void Startup()
        {
            base.Startup();

            _gameChat = new ChatBox();
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
            _gameChat.DefaultChatFormat = "say \"{0}\"";

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusChat,
                InputCmdHandler.FromDelegate(_ => FocusChat(_gameChat)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusOOC,
                InputCmdHandler.FromDelegate(_ => FocusChannel(_gameChat, ChatChannel.OOC)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusLocalChat,
                InputCmdHandler.FromDelegate(_ => FocusChannel(_gameChat, ChatChannel.Local)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusRadio,
                InputCmdHandler.FromDelegate(_ => FocusChannel(_gameChat, ChatChannel.Radio)));

            _inputManager.SetInputCommand(ContentKeyFunctions.FocusAdminChat,
                InputCmdHandler.FromDelegate(_ => FocusChannel(_gameChat, ChatChannel.AdminChat)));

            _inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
                InputCmdHandler.FromDelegate(_ => CycleChatChannel(_gameChat, true)));

            _inputManager.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
                InputCmdHandler.FromDelegate(_ => CycleChatChannel(_gameChat, false)));

            SetupPresenters();

            _eyeManager.MainViewport = Viewport.Viewport;
        }

        public override void Shutdown()
        {
            DisposePresenters();

            base.Shutdown();

            _gameChat?.Dispose();
            Viewport.Dispose();
            _gameHud.RootControl.Orphan();
            // Clear viewport to some fallback, whatever.
            _eyeManager.MainViewport = _userInterfaceManager.MainViewport;

        }

        /// <summary>
        /// All UI Presenters should be constructed in here.
        /// </summary>
        private void SetupPresenters()
        {
            _constructionMenu = new ConstructionMenuPresenter(_gameHud);
        }

        /// <summary>
        /// All UI Presenters should be disposed in here.
        /// </summary>
        private void DisposePresenters()
        {
            _constructionMenu?.Dispose();
        }

        internal static void FocusChat(ChatBox chat)
        {
            if (chat.UserInterfaceManager.KeyboardFocused != null)
            {
                return;
            }

            chat.Input.IgnoreNext = true;
            chat.Input.GrabKeyboardFocus();
        }
        internal static void FocusChannel(ChatBox chat, ChatChannel channel)
        {
            if (chat.UserInterfaceManager.KeyboardFocused != null)
            {
                return;
            }

            chat.Input.IgnoreNext = true;
            chat.SelectChannel(channel);
        }

        internal static void CycleChatChannel(ChatBox chat, bool forward)
        {
            chat.Input.IgnoreNext = true;
            var channels = chat.SelectableChannels;
            var idx = channels.IndexOf(chat.SelectedChannel);
            if (forward)
            {
                idx++;
                idx = MathHelper.Mod(idx, channels.Count());
            }
            else
            {
                idx--;
                idx = MathHelper.Mod(idx, channels.Count());
            }

            chat.SelectChannel(channels[idx]);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            Viewport.Viewport.Eye = _eyeManager.CurrentEye;
        }
    }
}
