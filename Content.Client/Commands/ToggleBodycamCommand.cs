using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Content.Client.Administration.Managers;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class ToggleBodycamCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;

    public override string Command => "togglebodycam";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Only admins can toggle this CVar
        if (!_admin.IsAdmin())
        {
            shell.WriteLine("Only admins can toggle the bodycam effect.");
            return;
        }

        var cvar = CCVars.HudBodycamEnabled;
        var old = _cfg.GetCVar(cvar);
        _cfg.SetCVar(cvar, !old);
        shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-notify", ("state", _cfg.GetCVar(cvar))));
    }
}
