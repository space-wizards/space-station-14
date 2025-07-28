using Content.Server.Administration;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Power.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class PowerValidateCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PowerNetSystem _powerNet = null!;

    public override string Command => "power_validate";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            _powerNet.Validate();
        }
        catch (Exception e)
        {
            shell.WriteLine(LocalizationManager.GetString("cmd-power_validate-error", ("err", e.ToString())));
            return;
        }

        shell.WriteLine(LocalizationManager.GetString("cmd-power_validate-success"));
    }
}
