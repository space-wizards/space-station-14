using System.Diagnostics.CodeAnalysis;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Info;
using Content.Shared.Administration;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Bwoink;

[UsedImplicitly]
public sealed class AHelpUIController: UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<BwoinkSystem>
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    private BwoinkSystem? _bwoinkSystem;
    private MenuButton? AhelpButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.AHelpButton;
    private IAHelpUIHandler? _uiHelper;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_uiHelper == null);
        _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<AHelpUIController>();
    }

    public void UnloadButton()
    {
        if (AhelpButton == null)
        {
            return;
        }

        AhelpButton.OnPressed -= AHelpButtonPressed;
    }

    public void LoadButton()
    {
        if (AhelpButton == null)
        {
            return;
        }

        AhelpButton.OnPressed += AHelpButtonPressed;
    }

    private void OnAdminStatusUpdated()
    {
        if (_uiHelper is not { IsOpen: true })
            return;
        EnsureUIHelper();
    }

    private void AHelpButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        EnsureUIHelper();
        _uiHelper!.ToggleWindow();
    }

    public void OnStateExited(GameplayState state)
    {
        SetAHelpPressed(false);
        _adminManager.AdminStatusUpdated -= OnAdminStatusUpdated;
        _uiHelper?.Dispose();
        _uiHelper = null;
        CommandBinds.Unregister<AHelpUIController>();
    }
    public void OnSystemLoaded(BwoinkSystem system)
    {
        _bwoinkSystem = system;
        _bwoinkSystem.OnBwoinkTextMessageRecieved += RecievedBwoink;
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        DebugTools.Assert(_bwoinkSystem != null);
        _bwoinkSystem!.OnBwoinkTextMessageRecieved -= RecievedBwoink;
        _bwoinkSystem = null;
    }

    private void SetAHelpPressed(bool pressed)
    {
        if (AhelpButton == null || AhelpButton.Pressed == pressed)
            return;
        AhelpButton.StyleClasses.Remove(MenuButton.StyleClassRedTopButton);
        AhelpButton.Pressed = pressed;
    }

    private void RecievedBwoink(object? sender, SharedBwoinkSystem.BwoinkTextMessage message)
    {
        Logger.InfoS("c.s.go.es.bwoink", $"@{message.UserId}: {message.Text}");
        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        if (localPlayer.UserId != message.TrueSender)
        {
            SoundSystem.Play("/Audio/Effects/adminhelp.ogg", Filter.Local());
            _clyde.RequestWindowAttention();
        }

        EnsureUIHelper();
        if (!_uiHelper!.IsOpen)
        {
            AhelpButton?.StyleClasses.Add(MenuButton.StyleClassRedTopButton);
        }
        _uiHelper!.Receive(message);
    }

    public void EnsureUIHelper()
    {
        var isAdmin = _adminManager.HasFlag(AdminFlags.Adminhelp);

        if (_uiHelper != null && _uiHelper.IsAdmin == isAdmin)
            return;

        _uiHelper?.Dispose();
        var ownerUserId = _playerManager!.LocalPlayer!.UserId;
        _uiHelper = isAdmin ? new AdminAHelpUIHandler(ownerUserId) : new UserAHelpUIHandler(ownerUserId);

        _uiHelper.SendMessageAction = (userId, textMessage) => _bwoinkSystem?.Send(userId, textMessage);
        _uiHelper.OnClose += () => { SetAHelpPressed(false); };
        _uiHelper.OnOpen +=  () => { SetAHelpPressed(true); };
        SetAHelpPressed(_uiHelper.IsOpen);
    }

    public void Close()
    {
        _uiHelper?.Close();
    }

    public void Open()
    {
        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        EnsureUIHelper();
        if (_uiHelper!.IsOpen)
            return;
        _uiHelper!.Open(localPlayer.UserId);
    }
    public void Open(NetUserId userId)
    {
        EnsureUIHelper();
        if (!_uiHelper!.IsAdmin)
            return;
        _uiHelper?.Open(userId);
    }
    public void ToggleWindow()
    {
        EnsureUIHelper();
        _uiHelper?.ToggleWindow();
    }
}

public interface IAHelpUIHandler: IDisposable
{
    public bool IsAdmin { get; }
    public bool IsOpen { get; }
    public void Receive(SharedBwoinkSystem.BwoinkTextMessage message);
    public void Close();
    public void Open(NetUserId netUserId);
    public void ToggleWindow();
    public event Action OnClose;
    public event Action OnOpen;
    public Action<NetUserId, string>? SendMessageAction { get; set; }
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
    public bool IsOpen => _window is { Disposed: false, IsOpen: true };
    private BwoinkWindow? _window;

    public void Receive(SharedBwoinkSystem.BwoinkTextMessage message)
    {
        var window = EnsurePanel(message.UserId);
        window.ReceiveLine(message);
        _window?.OnBwoink(message.UserId);
    }

    public void Close()
    {
        _window?.Close();
    }

    public void ToggleWindow()
    {
        EnsurePanel(_ownerId);
        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCentered();
        }
    }

    public event Action? OnClose;
    public event Action? OnOpen;
    public Action<NetUserId, string>? SendMessageAction { get; set; }

    public void Open(NetUserId channelId)
    {
        SelectChannel(channelId);
        _window?.OpenCentered();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = new BwoinkWindow(this);
        _window.OnClose += () => { OnClose?.Invoke(); };
        _window.OnOpen += () => { OnOpen?.Invoke(); };
    }
    public BwoinkPanel EnsurePanel(NetUserId channelId)
    {
        EnsureWindow();

        if (_activePanelMap.TryGetValue(channelId, out var existingPanel))
            return existingPanel;

        _activePanelMap[channelId] = existingPanel = new BwoinkPanel(text => SendMessageAction?.Invoke(channelId, text));
        existingPanel.Visible = false;
        if (!_window!.BwoinkArea.Children.Contains(existingPanel))
            _window.BwoinkArea.AddChild(existingPanel);

        return existingPanel;
    }
    public bool TryGetChannel(NetUserId ch, [NotNullWhen(true)] out BwoinkPanel? bp) => _activePanelMap.TryGetValue(ch, out bp);

    private void SelectChannel(NetUserId uid)
    {
        EnsurePanel(uid);
        _window!.SelectChannel(uid);
    }

    public void Dispose()
    {
        _window?.Dispose();
        _window = null;
        _activePanelMap.Clear();
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

    public void Receive(SharedBwoinkSystem.BwoinkTextMessage message)
    {
        DebugTools.Assert(message.UserId == _ownerId);
        EnsureInit();
        _chatPanel!.ReceiveLine(message);
        _window!.OpenCentered();
    }

    public void Close()
    {
        _window?.Close();
    }

    public void ToggleWindow()
    {
        EnsureInit();
        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCentered();
        }
    }

    public event Action? OnClose;
    public event Action? OnOpen;
    public Action<NetUserId, string>? SendMessageAction { get; set; }

    public void Open(NetUserId channelId)
    {
        EnsureInit();
        _window!.OpenCentered();
    }

    private void EnsureInit()
    {
        if (_window is { Disposed: false })
            return;
        _chatPanel = new BwoinkPanel(text => SendMessageAction?.Invoke(_ownerId, text));
        _window = new DefaultWindow()
        {
            TitleClass="windowTitleAlert",
            HeaderClass="windowHeaderAlert",
            Title=Loc.GetString("bwoink-user-title"),
            SetSize=(400, 200),
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
