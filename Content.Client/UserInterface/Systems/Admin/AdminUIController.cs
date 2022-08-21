using Content.Client.Administration.Managers;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.Tabs.PlayerTab;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs;
using Content.Shared.Input;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Admin;

public sealed class AdminUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IClientConGroupController _conGroups = default!;
    [Dependency] private readonly IClientConsoleHost _conHost = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly INetManager _net = default!;

    [UISystemDependency] private readonly VerbSystem _verbs = default!;

    private AdminMenuWindow? _window;

    private MenuButton AdminButton => UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().AdminButton;

    public void OnStateEntered(GameplayState state)
    {
        // Reset the AdminMenu Window on disconnect
        _net.Disconnect += (_, _) => ResetWindow();
        if (_window == null) //create the window if it is null
            CreateWindow();
        _input.SetInputCommand(ContentKeyFunctions.OpenAdminMenu,
            InputCmdHandler.FromDelegate(_ => Toggle()));

        _admin.AdminStatusUpdated += AdminStatusUpdated;
        AdminButton.OnPressed += AdminButtonPressed;
        AdminStatusUpdated();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<AdminUIController>();
    }

    private void AdminStatusUpdated()
    {
        if (_window == null)
            return;
        AdminButton.Visible = CanOpen();
    }

    private void AdminButtonPressed(ButtonEventArgs args)
    {
        Toggle();
    }

    private void Open()
    {
        _window!.Open();
        AdminButton.Pressed = true;
    }

    private void Close()
    {
        _window?.Close();
        AdminButton.Pressed = false;
    }

    /// <summary>
    /// Checks if the player can open the window
    /// </summary>
    /// <returns>True if the player is allowed</returns>
    public bool CanOpen()
    {
        return _conGroups.CanAdminMenu();
    }

    /// <summary>
    /// Checks if the player can open the window and tries to open it
    /// </summary>
    public void TryOpen()
    {
        if (CanOpen())
            Open();
    }

    public void Toggle()
    {
        if (_window is {IsOpen: true})
        {
            Close();
        }
        else
        {
            TryOpen();
        }
    }

    private void CreateWindow()
    {
        _window = UIManager.CreateWindow<AdminMenuWindow>();
        LayoutContainer.SetAnchorPreset(_window,LayoutContainer.LayoutPreset.Center);
        _window.PlayerTabControl.OnEntryPressed += PlayerTabEntryPressed;
        _window.OnClose += Close;
    }

    public void ResetWindow()
    {
        _window?.Close();
        _window?.Dispose();
        _window = null;
    }

    private void PlayerTabEntryPressed(ButtonEventArgs args)
    {
        if (args.Button is not PlayerTabEntry button
            || button.PlayerUid == null)
            return;

        var uid = button.PlayerUid.Value;
        var function = args.Event.Function;

        if (function == EngineKeyFunctions.UIClick)
            _conHost.ExecuteCommand($"vv {uid}");
        else if (function == ContentKeyFunctions.OpenContextMenu)
            _verbs.VerbMenu.OpenVerbMenu(uid, true);
        else
            return;

        args.Event.Handle();
    }
}
