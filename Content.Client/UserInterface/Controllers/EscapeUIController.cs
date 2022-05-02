using Content.Client.Gameplay;
using Content.Client.MainMenu;
using Content.Client.Options.UI;
using Content.Client.UserInterface.Controls;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;
using MenuBar = Content.Client.UserInterface.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class EscapeUIController : UIController, IOnStateChanged<GameplayState>, IOnStateChanged<MainScreen>
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IInputManager _input = default!;

    private EscapeMenu? _window;

    private MenuButton? EscapeButton => UIManager.GetActiveUIWidgetOrNull<MenuBar>()?.EscapeButton;

    public void OnStateChanged(GameplayState state)
    {
        if (EscapeButton != null)
        {
            EscapeButton.OnPressed += EscapeButtonPressed;
        }
    }

    public void OnStateChanged(MainScreen state)
    {
        _input.SetInputCommand(EngineKeyFunctions.EscapeMenu,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
    }

    private void EscapeButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void OpenWindow()
    {
        _window = new EscapeMenu();

        _window.AHelpButton.OnPressed += _ => {
            _console.ExecuteCommand("openahelp");
            CloseWindow();
        };

        _window.DisconnectButton.OnPressed += _ =>
        {
            _console.ExecuteCommand("disconnect");
            CloseWindow();
        };

        _window.OptionsButton.OnPressed += _ =>
        {
            new OptionsMenu().OpenCentered();
            CloseWindow();
        };

        _window.QuitButton.OnPressed += _ =>
        {
            _console.ExecuteCommand("quit");
            CloseWindow();
        };

        _window.OpenCentered();

        if (EscapeButton != null)
            EscapeButton.Pressed = true;
    }

    private void CloseWindow()
    {
        _window?.Close();

        if (EscapeButton != null)
            EscapeButton.Pressed = false;
    }

    private void ToggleWindow()
    {
        if (_window?.IsOpen != true)
            OpenWindow();
        else
            CloseWindow();
    }
}
