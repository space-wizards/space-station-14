using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class SetLOOCCommand : IConsoleCommand
{
    public string Command => "setlooc";
    public string Description => Loc.GetString("set-looc-command-description");
    public string Help => Loc.GetString("set-looc-command-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("set-looc-command-too-many-arguments-error"));
            return;
        }

        var looc = cfg.GetCVar(CCVars.LoocEnabled);

        if (args.Length == 0)
        {
            looc = !looc;
        }

        if (args.Length == 1 && !bool.TryParse(args[0], out looc))
        {
            shell.WriteError(Loc.GetString("set-looc-command-invalid-argument-error"));
            return;
        }

        cfg.SetCVar(CCVars.LoocEnabled, looc);

        shell.WriteLine(Loc.GetString(looc ? "set-looc-command-looc-enabled" : "set-looc-command-looc-disabled"));
    }
}
