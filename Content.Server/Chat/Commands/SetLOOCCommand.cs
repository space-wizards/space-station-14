using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class SetLoocCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    public override string Command => "setlooc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        var looc = _configManager.GetCVar(CCVars.LoocEnabled);

        if (args.Length == 0)
        {
            looc = !looc;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out looc))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        _configManager.SetCVar(CCVars.LoocEnabled, looc);

        shell.WriteLine(Loc.GetString(looc ? "cmd-setlooc-looc-enabled" : "cmd-setlooc-looc-disabled"));
    }
}
