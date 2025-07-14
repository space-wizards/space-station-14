using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class ToggleDisallowLateJoinCommand : LocalizedCommands
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        public override string Command => "toggledisallowlatejoin";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString($"shell-need-exactly-one-argument"));
                return;
            }

            if (bool.TryParse(args[0], out var result))
            {
                _configManager.SetCVar(CCVars.GameDisallowLateJoins, bool.Parse(args[0]));
                shell.WriteLine(Loc.GetString(result ? "cmd-toggledisallowlatejoin-disabled" : "cmd-toggledisallowlatejoin-enabled"));
            }
            else
                shell.WriteLine(Loc.GetString($"shell-invalid-bool"));
        }
    }
}
