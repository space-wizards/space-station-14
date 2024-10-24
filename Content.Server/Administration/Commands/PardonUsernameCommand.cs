using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class PardonUsernameCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;

    public override string Command => "pardonusername";
    public override string Help => Loc.GetString("cmd-pardonusername-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-need-minimum-one-argument"));
            return;
        }

        foreach (var arg in args)
        {
            if (!int.TryParse(arg, out var banId))
            {
                shell.WriteLine($"{Loc.GetString("shell-argument-must-be-integer")}.\n{Help}");
                continue;
            }

            shell.WriteLine(Loc.GetString("cmd-pardonusername-send", ("id", banId)));

            _usernameRules.RemoveUsernameRule(banId, shell.Player?.UserId);
        }
    }
}
