using Content.Client.Options.UI;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class OptionsUIController : UIController
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IConsoleHost _con = default!;

    public override void Initialize()
    {
        _con.RegisterCommand(
            "options",
            Loc.GetString("cmd-options-desc"),
            Loc.GetString("cmd-options-help"),
            OptionsCommand);

        _con.RegisterCommand(
            "advancedsettings",
            Loc.GetString("cmd-advanced-settings-desc"),
            "",
            AdvancedSettingsCommand);
    }

    private void OptionsCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            ToggleWindow();
            return;
        }
        OpenWindow();

        if (!int.TryParse(args[0], out var tab))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
            return;
        }

        _optionsWindow.Tabs.CurrentTab = tab;
    }

    private void AdvancedSettingsCommand(IConsoleShell shell, string argStr, string[] args)
    {
        var newValue = !_config.GetCVar(CCVars.AdvancedSettings);
        _config.SetCVar(CCVars.AdvancedSettings, newValue, true);
        shell.WriteLine(Loc.GetString("cmd-advanced-settings-log", ("value", newValue)));

        _optionsWindow.UpdateTabs();
    }

    private OptionsMenu _optionsWindow = default!;

    private void EnsureWindow()
    {
        if (_optionsWindow is { Disposed: false })
            return;

        _optionsWindow = UIManager.CreateWindow<OptionsMenu>();
    }

    public void OpenWindow()
    {
        EnsureWindow();

        _optionsWindow.UpdateTabs();

        _optionsWindow.OpenCentered();
        _optionsWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_optionsWindow.IsOpen)
        {
            _optionsWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
