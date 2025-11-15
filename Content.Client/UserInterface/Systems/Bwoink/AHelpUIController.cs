using Content.Client.Administration.Managers;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.CCVar;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Bwoink;

[UsedImplicitly]
public sealed class AHelpUIController: UIController, IOnStateChanged<GameplayState>, IOnStateChanged<LobbyState>
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly ClientBwoinkManager _bwoinkManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    private MenuButton? GameAHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.AHelpButton;
    private Button? LobbyAHelpButton => (UIManager.ActiveScreen as LobbyGui)?.AHelpButton;
    private bool _hasUnreadAHelp;
    private bool _bwoinkSoundEnabled;
    private BwoinkWindow? _window;
    private bool IsReal => _window is { Disposed: false };
    private bool IsVisible => _window is { IsOpen: true, Disposed: false };

    public override void Initialize()
    {
        base.Initialize();

        _config.OnValueChanged(CCVars.BwoinkSoundEnabled, v => _bwoinkSoundEnabled = v, true);

        _input.SetInputCommand(ContentKeyFunctions.OpenAHelp,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));


        _bwoinkManager.MessageReceived += MessageReceived;
        _adminManager.AdminStatusUpdated += AdminStatusChanged;
    }

    private void AdminStatusChanged()
    {
        if (_window == null)
            return;

        _window.Close();
        _window = null;
    }

    private void MessageReceived(ProtoId<BwoinkChannelPrototype> sender, (NetUserId person, BwoinkMessage message) args)
    {
        // are we receiving our own bwoink, if so, don't flash, don't play a sound etc. The player already KNOWS that a bwoink was sent.
        if (args.message.Sender == _playerManager.LocalSession!.Name)
            return;

        // are we a manager for this channel? if so, we do not open the window itself.
        var isManager = _bwoinkManager.CanManageChannel(sender, _playerManager.LocalSession);
        if (!isManager)
        {
            var wasVisible = IsVisible;

            EnsureUIHelper();
            // We need to ensure we open on the correct tab, *however* we do not want to switch tabs if the UI is already open:
            // Reason being that we do not want to switch channels while a client is typing.
            if (!wasVisible)
            {
                _window!.SwitchToChannel(sender);
            }
        }

        if (!_clyde.IsFocused) // wake up samurai, we have a city to burn
            _clyde.RequestWindowAttention();

        if (_bwoinkSoundEnabled
            && _bwoinkManager.CachedSounds.TryGetValue(sender, out var sound)
            && !args.message.Flags.HasFlag(MessageFlags.Silent))
        {
            sound?.Restart();
        }

        if (!IsVisible && isManager)
        {
            var info = _bwoinkManager.GetOrCreatePlayerPropertiesForChannel(sender, args.person);
            info.Unread++;
            info.LastMessage = args.message.SentAt;
        }
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

    private void AHelpButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
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

        UIManager.ClickSound();
        UnreadAHelpRead();
    }

    private void ToggleWindow()
    {
        if (IsVisible)
        {
            _window?.Close();
            SetAHelpPressed(false);
        }
        else
        {
            if (!_netManager.IsConnected)
                return;

            EnsureUIHelper();
            SetAHelpPressed(true);
        }
    }

    public void EnsureUIHelper()
    {
        if (IsVisible)
            return;

        if (!_netManager.IsConnected)
            return;

        if (IsReal)
        {
            // no need to remake it if we already have one, just open it duh
            _window!.OpenCentered();
            return;
        }

        _window = new BwoinkWindow(_bwoinkManager, _prototypeManager, _playerManager, _localizationManager);
        _window.OpenCentered();
        _window.OnClose += () =>
        {
            SetAHelpPressed(false);
        };

        SetAHelpPressed(true);
    }

    public void Open()
    {
        throw new NotImplementedException();
    }

    public void Open(NetUserId userId)
    {
        throw new NotImplementedException();
    }

    public void PopOut()
    {
        throw new NotImplementedException();
    }

    private void UnreadAHelpReceived()
    {
        GameAHelpButton?.StyleClasses.Add(StyleClass.Negative);
        LobbyAHelpButton?.StyleClasses.Add(StyleClass.Negative);
        _hasUnreadAHelp = true;
    }

    private void UnreadAHelpRead()
    {
        GameAHelpButton?.StyleClasses.Remove(StyleClass.Negative);
        LobbyAHelpButton?.StyleClasses.Remove(StyleClass.Negative);
        _hasUnreadAHelp = false;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (GameAHelpButton != null)
        {
            GameAHelpButton.OnPressed -= AHelpButtonPressed;
            GameAHelpButton.OnPressed += AHelpButtonPressed;
            GameAHelpButton.Pressed = IsVisible;

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
            LobbyAHelpButton.Pressed = IsVisible;

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
