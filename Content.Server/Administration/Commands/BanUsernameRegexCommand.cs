using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class BanUsernameRegexCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public override string Command => "banusernameregex";

    public override string Help => Loc.GetString("cmd-ban-username-regex-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 3)
        {
            shell.WriteError(LocalizationManager.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 3)));
            return;
        }

        var issuer = shell.Player?.UserId;
        string usernameRule = args[0];
        string message = "";
        bool ban = false;

        if (args.Length > 1)
        {
            message = args[1];
        }
        else
        {
            message = usernameRule;
        }

        if (args.Length > 2 && !bool.TryParse(args[2], out ban))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
            return;
        }

        _usernameRules.CreateUsernameRule(true, usernameRule, message, issuer, ban);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-ban-username-hint-regex"));

            case 2:
                return CompletionResult.FromHint(LocalizationManager.GetString("cmd-ban-username-hint-reason"));

            case 3:
                // optional ban conversion
                var convertToBan = new CompletionOption[]
                {
                    new("true", LocalizationManager.GetString("cmd-ban-username-hint-ban")),
                    new("false", LocalizationManager.GetString("cmd-ban-username-hint-no-ban")),
                };

                return CompletionResult.FromHintOptions(convertToBan, Loc.GetString("cmd-ban-username-hint-upgrade-ban"));

            default:
                break;
        }

        return CompletionResult.Empty;
    }
}
