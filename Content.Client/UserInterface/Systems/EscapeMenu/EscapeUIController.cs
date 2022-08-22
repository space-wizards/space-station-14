using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Client.Links;
using Content.Client.MainMenu;
using Content.Client.Options.UI;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using TerraFX.Interop.Windows;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class EscapeUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IUriOpener _uri = default!;

    private Options.UI.EscapeMenu? _escapeWindow;
    private RulesAndInfoWindow? _rulesAndInfoWindow;
    private OptionsMenu? _optionsWindow;

    private MenuButton? _escapeButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_escapeWindow == null);

        _escapeButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().EscapeButton;
        _escapeButton.OnPressed += EscapeButtonOnOnPressed;

        _escapeWindow = UIManager.CreateWindow<Options.UI.EscapeMenu>();
        _escapeWindow.OnClose += OnEscapeClosed;
        _escapeWindow.RulesButton.OnPressed += _ =>
        {
            CloseWindow();
            _rulesAndInfoWindow ??= UIManager.CreateWindow<RulesAndInfoWindow>();
            _rulesAndInfoWindow.OpenCentered();
        };

        _escapeWindow.DisconnectButton.OnPressed += _ =>
        {
            CloseWindow();
            _console.ExecuteCommand("disconnect");
        };

        _escapeWindow.OptionsButton.OnPressed += _ =>
        {
            CloseWindow();
            _optionsWindow ??= UIManager.CreateWindow<OptionsMenu>();
            _optionsWindow.OpenCentered();
        };

        _escapeWindow.QuitButton.OnPressed += _ =>
        {
            CloseWindow();
            _console.ExecuteCommand("quit");
        };

        _escapeWindow.WikiButton.OnPressed += _ =>
        {
            _uri.OpenUri(UILinks.Wiki);
        };

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EscapeMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<EscapeUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _escapeWindow?.DisposeAllChildren();
        _escapeWindow = null;
        CommandBinds.Unregister<EscapeUIController>();
        if (_escapeButton == null)
            return;
        _escapeButton.OnPressed -= EscapeButtonOnOnPressed;
        _escapeButton = null;
    }

    private void EscapeButtonOnOnPressed(ButtonEventArgs obj)
    {
        ToggleWindow();
    }

    private void OnEscapeClosed()
    {
        _escapeButton!.Pressed = false;
    }

    private void CloseWindow()
    {
        _escapeWindow?.Close();
    }

    private void ToggleWindow()
    {
        if (_escapeWindow == null)
            return;
        if (_escapeWindow.IsOpen != true)
        {
            _escapeWindow.OpenCentered();
            _escapeButton!.Pressed = true;
        }
        else
        {
            CloseWindow();
        }
    }
}
