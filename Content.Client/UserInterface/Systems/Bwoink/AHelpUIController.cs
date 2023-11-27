using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Administration;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Bwoink;

[UsedImplicitly]
public sealed class AHelpUIController: UIController, IOnSystemChanged<BwoinkSystem>, IOnStateChanged<GameplayState>, IOnStateChanged<LobbyState>
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [UISystemDependency] private readonly SharedAudioSystem _audio = default!;

    private BwoinkSystem? _bwoinkSystem;
    private MenuButton? GameAHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.AHelpButton;
    private Button? LobbyAHelpButton => (UIManager.ActiveScreen as LobbyGui)?.AHelpButton;
    public IAHelpUIHandler? UIHelper;
    private bool _discordRelayActive;
    private bool _hasUnreadAHelp;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BwoinkDiscordRelayUpdated>(DiscordRelayUpdated);
        SubscribeNetworkEvent<BwoinkPlayerTypingUpdated>(PeopleTypingUpdated);

        _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
    }

    public void UnloadButton()
    {
        if (GameAHelpButton != null)
            GameAHelpButton.OnPressed -= AHelpButtonPressed;

        if (LobbyAHelpButton != null)
            LobbyAHelpButton.OnPressed -= AHelpButtonPressed;
    }

    public void LoadButton()
    {
        if (GameAHelpButton != null)
            GameAHelpButton.OnPressed += AHelpButtonPressed;

        if (LobbyAHelpButton != null)
            LobbyAHelpButton.OnPressed += AHelpButtonPressed;
    }

    private void OnAdminStatusUpdated()
    {
        if (UIHelper is not { IsOpen: true })
            return;
        EnsureUIHelper();
    }

    private void AHelpButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        EnsureUIHelper();
        UIHelper!.ToggleWindow();
    }

    public void OnSystemLoaded(BwoinkSystem system)
    {
        _bwoinkSystem = system;
        _bwoinkSystem.OnBwoinkTextMessageRecieved += ReceivedBwoink;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<AHelpUIController>();
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        CommandBinds.Unregister<AHelpUIController>();

        DebugTools.Assert(_bwoinkSystem != null);
        _bwoinkSystem!.OnBwoinkTextMessageRecieved -= ReceivedBwoink;
        _bwoinkSystem = null;
    }

    private void SetAHelpPressed(bool pressed)
    {
        if (GameAHelpButton != null)
        {
            GameAHelpButton.Pressed = pressed;
        }

        if (LobbyAHelpButton != null)
        {
            LobbyAHelpButton.Pressed = pressed;
        }

        UnreadAHelpRead();
    }

    private void ReceivedBwoink(object? sender, SharedBwoinkSystem.BwoinkTextMessage message)
    {
        Logger.InfoS("c.s.go.es.bwoink", $"@{message.UserId}: {message.Text}");
        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        if (localPlayer.UserId != message.TrueSender)
        {
            _audio.PlayGlobal("/Audio/Effects/adminhelp.ogg", Filter.Local(), false);
            _clyde.RequestWindowAttention();
        }

        EnsureUIHelper();

        if (!UIHelper!.IsOpen)
        {
            UnreadAHelpReceived();
        }

        UIHelper!.Receive(message);
    }

    private void DiscordRelayUpdated(BwoinkDiscordRelayUpdated args, EntitySessionEventArgs session)
    {
        _discordRelayActive = args.DiscordRelayEnabled;
        UIHelper?.DiscordRelayChanged(_discordRelayActive);
    }

    private void PeopleTypingUpdated(BwoinkPlayerTypingUpdated args, EntitySessionEventArgs session)
    {
        UIHelper?.PeopleTypingUpdated(args);
    }

    public void EnsureUIHelper()
    {
        var isAdmin = _adminManager.HasFlag(AdminFlags.Adminhelp);

        if (UIHelper != null && UIHelper.IsAdmin == isAdmin)
            return;

        UIHelper?.Dispose();
        var ownerUserId = _playerManager.LocalPlayer!.UserId;
        UIHelper = isAdmin ? new AdminAHelpUIHandler(ownerUserId) : new UserAHelpUIHandler(ownerUserId);
        UIHelper.DiscordRelayChanged(_discordRelayActive);

        UIHelper.SendMessageAction = (userId, textMessage) => _bwoinkSystem?.Send(userId, textMessage);
        UIHelper.InputTextChanged += (channel, text) => _bwoinkSystem?.SendInputTextUpdated(channel, text.Length > 0);
        UIHelper.OnClose += () => { SetAHelpPressed(false); };
        UIHelper.OnOpen +=  () => { SetAHelpPressed(true); };
        SetAHelpPressed(UIHelper.IsOpen);
    }

    public void Open()
    {
        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        EnsureUIHelper();
        if (UIHelper!.IsOpen)
            return;
        UIHelper!.Open(localPlayer.UserId, _discordRelayActive);
    }

    public void Open(NetUserId userId)
    {
        EnsureUIHelper();
        if (!UIHelper!.IsAdmin)
            return;
        UIHelper?.Open(userId, _discordRelayActive);
    }

    public void ToggleWindow()
    {
        EnsureUIHelper();
        UIHelper?.ToggleWindow();
    }

    public void PopOut()
    {
        EnsureUIHelper();
        if (UIHelper is not AdminAHelpUIHandler helper)
            return;

        if (helper.Window == null || helper.Control == null)
        {
            return;
        }

        helper.Control.Orphan();
        helper.Window.Dispose();
        helper.Window = null;
        helper.EverOpened = false;

        var monitor = _clyde.EnumerateMonitors().First();

        helper.ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = "Admin Help",
            Monitor = monitor,
            Width = 900,
            Height = 500
        });

        helper.ClydeWindow.RequestClosed += helper.OnRequestClosed;
        helper.ClydeWindow.DisposeOnClose = true;

        helper.WindowRoot = _uiManager.CreateWindowRoot(helper.ClydeWindow);
        helper.WindowRoot.AddChild(helper.Control);

        helper.Control.PopOut.Disabled = true;
        helper.Control.PopOut.Visible = false;
    }

    private void UnreadAHelpReceived()
    {
        GameAHelpButton?.StyleClasses.Add(MenuButton.StyleClassRedTopButton);
        LobbyAHelpButton?.StyleClasses.Add(StyleNano.StyleClassButtonColorRed);
        _hasUnreadAHelp = true;
    }

    private void UnreadAHelpRead()
    {
        GameAHelpButton?.StyleClasses.Remove(MenuButton.StyleClassRedTopButton);
        LobbyAHelpButton?.StyleClasses.Remove(StyleNano.StyleClassButtonColorRed);
        _hasUnreadAHelp = false;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (GameAHelpButton != null)
        {
            GameAHelpButton.OnPressed -= AHelpButtonPressed;
            GameAHelpButton.OnPressed += AHelpButtonPressed;
            GameAHelpButton.Pressed = UIHelper?.IsOpen ?? false;

            if (_hasUnreadAHelp)
            {
                UnreadAHelpReceived();
            }
            else
            {
                UnreadAHelpRead();
            }
        }
    }

    public void OnStateExited(GameplayState state)
    {
        if (GameAHelpButton != null)
            GameAHelpButton.OnPressed -= AHelpButtonPressed;
    }

    public void OnStateEntered(LobbyState state)
    {
        if (LobbyAHelpButton != null)
        {
            LobbyAHelpButton.OnPressed -= AHelpButtonPressed;
            LobbyAHelpButton.OnPressed += AHelpButtonPressed;
            LobbyAHelpButton.Pressed = UIHelper?.IsOpen ?? false;

            if (_hasUnreadAHelp)
            {
                UnreadAHelpReceived();
            }
            else
            {
                UnreadAHelpRead();
            }
        }
    }

    public void OnStateExited(LobbyState state)
    {
        if (LobbyAHelpButton != null)
            LobbyAHelpButton.OnPressed -= AHelpButtonPressed;
    }
}

// please kill all this indirection
public interface IAHelpUIHandler : IDisposable
{
    public bool IsAdmin { get; }
    public bool IsOpen { get; }
    public void Receive(SharedBwoinkSystem.BwoinkTextMessage message);
    public void Close();
    public void Open(NetUserId netUserId, bool relayActive);
    public void ToggleWindow();
    public void DiscordRelayChanged(bool active);
    public void PeopleTypingUpdated(BwoinkPlayerTypingUpdated args);
    public event Action OnClose;
    public event Action OnOpen;
    public Action<NetUserId, string>? SendMessageAction { get; set; }
    public event Action<NetUserId, string>? InputTextChanged;
}
public sealed class AdminAHelpUIHandler : IAHelpUIHandler
{
    private readonly NetUserId _ownerId;
    public AdminAHelpUIHandler(NetUserId owner)
    {
        _ownerId = owner;
    }
    private readonly Dictionary<NetUserId, BwoinkPanel> _activePanelMap = new();
    public bool IsAdmin => true;
    public bool IsOpen => Window is { Disposed: false, IsOpen: true } || ClydeWindow is { IsDisposed: false };
    public bool EverOpened;

    public BwoinkWindow? Window;
    public WindowRoot? WindowRoot;
    public IClydeWindow? ClydeWindow;
    public BwoinkControl? Control;

    public void Receive(SharedBwoinkSystem.BwoinkTextMessage message)
    {
        var panel = EnsurePanel(message.UserId);
        panel.ReceiveLine(message);
        Control?.OnBwoink(message.UserId);
    }

    private void OpenWindow()
    {
        if (Window == null)
            return;

        if (EverOpened)
            Window.Open();
        else
            Window.OpenCentered();
    }

    public void Close()
    {
        Window?.Close();

        // popped-out window is being closed
        if (ClydeWindow != null)
        {
            ClydeWindow.RequestClosed -= OnRequestClosed;
            ClydeWindow.Dispose();
            // need to dispose control cause we cant reattach it directly back to the window
            // but orphan panels first so -they- can get readded when the window is opened again
            if (Control != null)
            {
                foreach (var (_, panel) in _activePanelMap)
                {
                    panel.Orphan();
                }
                Control?.Dispose();
            }
            // window wont be closed here so we will invoke ourselves
            OnClose?.Invoke();
        }
    }

    public void ToggleWindow()
    {
        EnsurePanel(_ownerId);

        if (IsOpen)
            Close();
        else
            OpenWindow();
    }

    public void DiscordRelayChanged(bool active)
    {
    }

    public void PeopleTypingUpdated(BwoinkPlayerTypingUpdated args)
    {
        if (_activePanelMap.TryGetValue(args.Channel, out var panel))
            panel.UpdatePlayerTyping(args.PlayerName, args.Typing);
    }

    public event Action? OnClose;
    public event Action? OnOpen;
    public Action<NetUserId, string>? SendMessageAction { get; set; }
    public event Action<NetUserId, string>? InputTextChanged;

    public void Open(NetUserId channelId, bool relayActive)
    {
        SelectChannel(channelId);
        OpenWindow();
    }

    public void OnRequestClosed(WindowRequestClosedEventArgs args)
    {
        Close();
    }

    private void EnsureControl()
    {
        if (Control is { Disposed: false })
            return;

        Window = new BwoinkWindow();
        Control = Window.Bwoink;
        Window.OnClose += () => { OnClose?.Invoke(); };
        Window.OnOpen += () =>
        {
            OnOpen?.Invoke();
            EverOpened = true;
        };

        // need to readd any unattached panels..
        foreach (var (_, panel) in _activePanelMap)
        {
            if (!Control!.BwoinkArea.Children.Contains(panel))
            {
                Control!.BwoinkArea.AddChild(panel);
            }
            panel.Visible = false;
        }
    }

    public void HideAllPanels()
    {
        foreach (var panel in _activePanelMap.Values)
        {
            panel.Visible = false;
        }
    }

    public BwoinkPanel EnsurePanel(NetUserId channelId)
    {
        EnsureControl();

        if (_activePanelMap.TryGetValue(channelId, out var existingPanel))
            return existingPanel;

        _activePanelMap[channelId] = existingPanel = new BwoinkPanel(text => SendMessageAction?.Invoke(channelId, text));
        existingPanel.InputTextChanged += text => InputTextChanged?.Invoke(channelId, text);
        existingPanel.Visible = false;
        if (!Control!.BwoinkArea.Children.Contains(existingPanel))
            Control.BwoinkArea.AddChild(existingPanel);

        return existingPanel;
    }
    public bool TryGetChannel(NetUserId ch, [NotNullWhen(true)] out BwoinkPanel? bp) => _activePanelMap.TryGetValue(ch, out bp);

    private void SelectChannel(NetUserId uid)
    {
        EnsurePanel(uid);
        Control!.SelectChannel(uid);
    }

    public void Dispose()
    {
        Window?.Dispose();
        Window = null;
        Control = null;
        _activePanelMap.Clear();
        EverOpened = false;
    }
}

public sealed class UserAHelpUIHandler : IAHelpUIHandler
{
    private readonly NetUserId _ownerId;
    public UserAHelpUIHandler(NetUserId owner)
    {
        _ownerId = owner;
    }
    public bool IsAdmin => false;
    public bool IsOpen => _window is { Disposed: false, IsOpen: true };
    private DefaultWindow? _window;
    private BwoinkPanel? _chatPanel;
    private bool _discordRelayActive;

    public void Receive(SharedBwoinkSystem.BwoinkTextMessage message)
    {
        DebugTools.Assert(message.UserId == _ownerId);
        EnsureInit(_discordRelayActive);
        _chatPanel!.ReceiveLine(message);
        _window!.OpenCentered();
    }

    public void Close()
    {
        _window?.Close();
    }

    public void ToggleWindow()
    {
        EnsureInit(_discordRelayActive);
        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCentered();
        }
    }

    // user can't pop out their window.
    public void PopOut()
    {
    }

    public void DiscordRelayChanged(bool active)
    {
        _discordRelayActive = active;

        if (_chatPanel != null)
        {
            _chatPanel.RelayedToDiscordLabel.Visible = active;
        }
    }

    public void PeopleTypingUpdated(BwoinkPlayerTypingUpdated args)
    {
    }

    public event Action? OnClose;
    public event Action? OnOpen;
    public Action<NetUserId, string>? SendMessageAction { get; set; }
    public event Action<NetUserId, string>? InputTextChanged;

    public void Open(NetUserId channelId, bool relayActive)
    {
        EnsureInit(relayActive);
        _window!.OpenCentered();
    }

    private void EnsureInit(bool relayActive)
    {
        if (_window is { Disposed: false })
            return;
        _chatPanel = new BwoinkPanel(text => SendMessageAction?.Invoke(_ownerId, text));
        _chatPanel.InputTextChanged += text => InputTextChanged?.Invoke(_ownerId, text);
        _chatPanel.RelayedToDiscordLabel.Visible = relayActive;
        _window = new DefaultWindow()
        {
            TitleClass="windowTitleAlert",
            HeaderClass="windowHeaderAlert",
            Title=Loc.GetString("bwoink-user-title"),
            MinSize = new Vector2(500, 300),
        };
        _window.OnClose += () => { OnClose?.Invoke(); };
        _window.OnOpen += () => { OnOpen?.Invoke(); };
        _window.Contents.AddChild(_chatPanel);
    }

    public void Dispose()
    {
        _window?.Dispose();
        _window = null;
        _chatPanel = null;
    }
}
