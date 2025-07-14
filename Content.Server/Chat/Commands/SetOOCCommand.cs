using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SetOOCCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    public override string Command => "setooc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        var ooc = _configManager.GetCVar(CCVars.OocEnabled);

        if (args.Length == 0)
        {
            ooc = !ooc;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out ooc))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        _configManager.SetCVar(CCVars.OocEnabled, ooc);

        shell.WriteLine(Loc.GetString(ooc ? "cmd-setooc-ooc-enabled" : "cmd-setooc-ooc-disabled"));
    }
}
