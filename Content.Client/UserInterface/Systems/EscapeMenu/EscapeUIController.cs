using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Client.UserInterface.Systems.Info;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class EscapeUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IUriOpener _uri = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ChangelogUIController _changelog = default!;
    [Dependency] private readonly InfoUIController _info = default!;
    [Dependency] private readonly OptionsUIController _options = default!;
    [Dependency] private readonly GuidebookUIController _guidebook = default!;

    private Options.UI.EscapeMenu? _window;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<Options.UI.EscapeMenu>();

        var button = UIManager.GetActiveUIWidget<GameTopMenuBar>().EscapeButton;
        _window.OnOpen += () => button.SetClickPressed(true);
        _window.OnClose += () => button.SetClickPressed(false);

        _window.ChangelogButton.OnPressed += _ =>
        {
            _window.Close();
            _changelog.ToggleWindow();
        };

        _window.RulesButton.OnPressed += _ =>
        {
            _window.Close();
            _info.OpenWindow();
        };

        _window.DisconnectButton.OnPressed += _ =>
        {
            _window.Close();
            _console.ExecuteCommand("disconnect");
        };

        _window.OptionsButton.OnPressed += _ =>
        {
            _window.Close();
            _options.OpenWindow();
        };

        _window.QuitButton.OnPressed += _ =>
        {
            _window.Close();
            _console.ExecuteCommand("quit");
        };

        _window.WikiButton.OnPressed += _ =>
        {
            _uri.OpenUri(_cfg.GetCVar(CCVars.InfoLinksWiki));
        };

        _window.GuidebookButton.OnPressed += _ =>
        {
            _guidebook.ToggleWindow();
        };

        // Hide wiki button if we don't have a link for it.
        _window.WikiButton.Visible = _cfg.GetCVar(CCVars.InfoLinksWiki) != "";

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EscapeMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<EscapeUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _window = null;

        CommandBinds.Unregister<EscapeUIController>();
    }

    /// <summary>
    /// Toggles the game menu.
    /// </summary>
    public void ToggleWindow()
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
            _window.Close();
        else
            _window.OpenCentered();
    }
}
