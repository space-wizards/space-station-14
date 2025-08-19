using System.Linq;
using Content.Client._Starlight.Managers;
using Content.Client._Starlight.MHelp;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Starlight.MHelp;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared._NullLink;

namespace Content.Client.UserInterface.Systems.Bwoink;

[UsedImplicitly]
public sealed class MHelpUIController : UIController, IOnSystemChanged<MentorSystem>, IOnStateChanged<GameplayState>, IOnStateChanged<LobbyState>
{
    [Dependency] private readonly INullLinkPlayerRolesManager _playerRoles = default!;
    [Dependency] private readonly ISharedNullLinkPlayerRolesReqManager _playerRolesReq = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [UISystemDependency] private readonly AudioSystem _audio = default!;

    private MentorSystem? _mentorSystem;
    private Controls.MenuButton? GameMHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.MHelpButton;
    private Button? LobbyMHelpButton => (UIManager.ActiveScreen as LobbyGui)?.MHelpButton;

    public IMHelpUIHandler? UIHelper;
    private bool _hasUnreadMHelp;
    private string? _mHelpSound;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MHelpTypingUpdated>(OnTypingUpdated);

        _playerRoles.PlayerRolesChanged += OnPlayerStatusUpdated;
        _config.OnValueChanged(StarlightCCVars.MHelpSound, v => _mHelpSound = v, true);
    }
    public void UnloadButton()
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed -= MHelpButtonPressed;

        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed -= MHelpButtonPressed;
    }

    public void LoadButton()
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed += MHelpButtonPressed;

        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed += MHelpButtonPressed;
    }

    private void OnPlayerStatusUpdated()
    {
        if (UIHelper is not { IsOpen: true })
            return;
        EnsureUIHelper();
    }

    private void MHelpButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        EnsureUIHelper();
        UIHelper!.ToggleWindow();
    }

    public void OnSystemLoaded(MentorSystem system)
    {
        _mentorSystem = system;
        _mentorSystem.OnMentoringTextMessageReceived += ReceivedMentoring;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenMHelp,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<MHelpUIController>();
    }

    public void OnSystemUnloaded(MentorSystem system)
    {
        CommandBinds.Unregister<MHelpUIController>();

        DebugTools.Assert(_mentorSystem != null);
        _mentorSystem!.OnMentoringTextMessageReceived -= ReceivedMentoring;
        _mentorSystem = null;
    }

    private void SetMHelpPressed(bool pressed)
    {
        if (GameMHelpButton != null)
        {
            GameMHelpButton.Pressed = pressed;
        }

        if (LobbyMHelpButton != null)
        {
            LobbyMHelpButton.Pressed = pressed;
        }

        UIManager.ClickSound();
        UnreadMHelpRead();
    }

    private void ReceivedMentoring(object? sender, SharedMentorSystem.MHelpTextMessage message)
    {
        var localPlayer = _playerManager.LocalSession;
        if (localPlayer == null)
            return;
        if (message.PlaySound && localPlayer.UserId != message.Sender && _config.GetCVar(StarlightCCVars.MHelpPing))
        {
            if (_mHelpSound != null)
                _audio.PlayGlobal(_mHelpSound, Filter.Local(), false);
            _clyde.RequestWindowAttention();
        }

        EnsureUIHelper();

        if (!UIHelper!.IsOpen)
        {
            UnreadMHelpReceived();
        }

        UIHelper!.Receive(message);
    }

    private void OnTypingUpdated(MHelpTypingUpdated args, EntitySessionEventArgs session)
    {
        UIHelper?.PeopleTypingUpdated(args);
    }

    public void EnsureUIHelper()
    {

        var isMentor = _playerManager.LocalSession is { } local && _playerRolesReq.IsMentor(local);
        var isAdmin = _adminManager.HasFlag(AdminFlags.Adminhelp);

        if (UIHelper != null && UIHelper.IsMentor == (isMentor || isAdmin))
            return;

        UIHelper?.Dispose();
        var ownerUserId = _playerManager.LocalUser!.Value;
        UIHelper = isMentor || isAdmin ? new MentorMHelpUIHandler(ownerUserId) : new UserMHelpUIHandler(ownerUserId);

        UIHelper.OnMessageSend += (ticket, textMessage, playSound) => _mentorSystem?.Send(ticket,  textMessage, playSound);
        UIHelper.OnInputTextChanged += (ticket, text) => _mentorSystem?.SendInputTextUpdated(ticket,  text.Length > 0);
        UIHelper.OnTicketClosed += ticket => _mentorSystem?.SendCloseTicket(ticket);
        UIHelper.OnTptoPressed += ticket => _mentorSystem?.SentTpto(ticket);
        UIHelper.OnClose += () => SetMHelpPressed(false);
        UIHelper.OnOpen += () => SetMHelpPressed(true);
        SetMHelpPressed(UIHelper.IsOpen);
    }

    public void Open()
    {
        var localUser = _playerManager.LocalUser;
        if (localUser == null)
            return;
        EnsureUIHelper();
        if (UIHelper!.IsOpen)
            return;
        UIHelper!.Open(localUser.Value);
    }

    public void Open(NetUserId userId)
    {
        EnsureUIHelper();
        if (!UIHelper!.IsMentor)
            return;
        UIHelper?.Open(userId);
    }

    public void ToggleWindow()
    {
        EnsureUIHelper();
        UIHelper?.ToggleWindow();
    }

    private void UnreadMHelpReceived()
    {
        GameMHelpButton?.StyleClasses.Add(Controls.MenuButton.StyleClassRedTopButton);
        LobbyMHelpButton?.StyleClasses.Add(StyleNano.StyleClassButtonColorRed);
        _hasUnreadMHelp = true;
    }

    private void UnreadMHelpRead()
    {
        GameMHelpButton?.StyleClasses.Remove(Controls.MenuButton.StyleClassRedTopButton);
        LobbyMHelpButton?.StyleClasses.Remove(StyleNano.StyleClassButtonColorRed);
        _hasUnreadMHelp = false;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (GameMHelpButton != null)
        {
            GameMHelpButton.OnPressed -= MHelpButtonPressed;
            GameMHelpButton.OnPressed += MHelpButtonPressed;
            GameMHelpButton.Pressed = UIHelper?.IsOpen ?? false;

            if (_hasUnreadMHelp)
            {
                UnreadMHelpReceived();
            }
            else
            {
                UnreadMHelpRead();
            }
        }
    }

    public void OnStateExited(GameplayState state)
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed -= MHelpButtonPressed;
    }

    public void OnStateEntered(LobbyState state)
    {
        if (LobbyMHelpButton != null)
        {
            LobbyMHelpButton.OnPressed -= MHelpButtonPressed;
            LobbyMHelpButton.OnPressed += MHelpButtonPressed;
            LobbyMHelpButton.Pressed = UIHelper?.IsOpen ?? false;

            if (_hasUnreadMHelp)
            {
                UnreadMHelpReceived();
            }
            else
            {
                UnreadMHelpRead();
            }
        }
    }

    public void OnStateExited(LobbyState state)
    {
        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed -= MHelpButtonPressed;
    }
}
