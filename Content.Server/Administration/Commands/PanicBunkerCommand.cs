using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PanicBunkerCommand : IConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string Command => "panicbunker";
    public string Description => "Enables or disables the panic bunker functionality.";
    public string Help => "panicbunker";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 0), ("upper", 1)));
            return;
        }

        var enabled = _cfg.GetCVar(CCVars.PanicBunkerEnabled);
        
        if (args.Length == 0)
        {
            enabled = !enabled;
        }
        
        if (args.Length == 1 && !bool.TryParse(args[0], out enabled))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
            return;
        }

        _cfg.SetCVar(CCVars.PanicBunkerEnabled, enabled);
        
        shell.WriteLine(Loc.GetString(enabled ? "panicbunker-command-enabled" : "panicbunker-command-disabled"));
    }
}
