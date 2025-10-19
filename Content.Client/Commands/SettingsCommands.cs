using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class OptionsCommand : LocalizedCommands
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "options";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var controller = _userInterfaceManager.GetUIController<OptionsUIController>();

        if (args.Length == 0)
        {
            controller.ToggleWindow();
            return;
        }

        controller.OpenWindow();

        if (!int.TryParse(args[0], out var tab))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-integer", ("arg", args[0])));
            return;
        }

        controller.SetWindowTab(tab);
    }
}

[AnyCommand]
public sealed class AdvancedSettingsCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "advancedsettings";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var newValue = !_config.GetCVar(CCVars.AdvancedSettings);
        _config.SetCVar(CCVars.AdvancedSettings, newValue, true);

        shell.WriteLine(Loc.GetString("cmd-advancedsettings-log", ("value", newValue)));

        _userInterfaceManager.GetUIController<OptionsUIController>().UpdateWindow();
    }
}
