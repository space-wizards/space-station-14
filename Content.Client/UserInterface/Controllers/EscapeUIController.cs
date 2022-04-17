using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.HUD.Widgets;
using Content.Client.Options.UI;
using Content.Client.UserInterface.Controls;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Controllers;

public sealed class EscapeUIController : UIController
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IHudManager _hud = default!;
    [Dependency] private readonly IInputManager _input = default!;

    private EscapeMenu? _window;

    private MenuButton EscapeButton => _hud.GetUIWidget<MenuBar>().EscapeButton;

    public override void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            EscapeButton.OnPressed += EscapeButtonPressed;
        }

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
        EscapeButton.Pressed = true;
    }

    private void CloseWindow()
    {
        _window?.Close();
        EscapeButton.Pressed = false;
    }

    private void ToggleWindow()
    {
        if (_window?.IsOpen != true)
            OpenWindow();
        else
            _window?.Close();
    }
}
