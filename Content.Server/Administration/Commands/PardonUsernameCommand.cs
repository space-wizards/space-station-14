using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class PardonUsernameCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;

    public override string Command => "pardonusername";
    public override string Help => $"Usage: {Command} <ban id 1> <ban id 2> ...";

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
                shell.WriteLine($"Unable to parse {arg} as a rule id integer.\n{Help}");
                continue;
            }

            shell.WriteLine($"Sending retire request rule {banId}");

            _usernameRules.RemoveUsernameRule(banId, shell.Player?.UserId);
        }
    }
}