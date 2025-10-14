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
using Robust.Client.UserInterface;
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
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly ClientBwoinkManager _bwoinkManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private MenuButton? GameAHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.AHelpButton;
    private Button? LobbyAHelpButton => (UIManager.ActiveScreen as LobbyGui)?.AHelpButton;
    private bool _hasUnreadAHelp;
    private bool _bwoinkSoundEnabled;
    private BwoinkWindow? _window;
    private bool IsOpen => _window is { Disposed: false };

    public override void Initialize()
    {
        base.Initialize();

        _config.OnValueChanged(CCVars.BwoinkSoundEnabled, v => _bwoinkSoundEnabled = v, true);

        _input.SetInputCommand(ContentKeyFunctions.OpenAHelp,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));


        _bwoinkManager.MessageReceived += MessageReceived;
    }

    private void MessageReceived(ProtoId<BwoinkChannelPrototype> sender, (NetUserId person, BwoinkMessage message) args)
    {
        // are we receiving our own bwoink, if so, don't flash, don't play a sound etc. The player already KNOWS that a bwoink was sent.
        if (args.message.Sender == _playerManager.LocalSession!.Name)
            return;

        // are we a manager for this channel? if so, we do not open the window itself.
        if (!_bwoinkManager.CanManageChannel(sender, _playerManager.LocalSession))
            EnsureUIHelper();

        if (!_clyde.IsFocused) // wake up samurai, we have a city to burn
            _clyde.RequestWindowAttention();

        if (_bwoinkSoundEnabled)
        {
            _bwoinkManager.CachedSounds[sender]?.Restart();
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
        if (IsOpen)
        {
            _window?.Close();
            _window = null;
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
        if (IsOpen)
            return;

        if (!_netManager.IsConnected)
            return;

        _window = new BwoinkWindow(_bwoinkManager, _prototypeManager, _playerManager);
        _window.OpenCentered();
        _window.OnClose += () =>
        {
            _window = null;
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
            GameAHelpButton.Pressed = IsOpen;

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
            LobbyAHelpButton.Pressed = IsOpen;

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
