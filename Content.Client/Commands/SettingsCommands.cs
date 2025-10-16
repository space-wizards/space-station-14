using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class AdvancedSettingsCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override string Command => "advancedsettings";

    public override string Description => Loc.GetString("cmd-advanced-settings-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var newValue = !_config.GetCVar(CCVars.AdvancedSettings);
        _config.SetCVar(CCVars.AdvancedSettings, newValue, true);

        shell.WriteLine(Loc.GetString("cmd-advanced-settings-log", ("value", newValue)));

        _userInterfaceManager.GetUIController<OptionsUIController>().UpdateWindow();
    }
}
