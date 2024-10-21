using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using UsernameHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class BanUsernameCommand : LocalizedCommands
{
    [Dependency] private readonly IUsernameRuleManager _usernameRules = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public override string Command => "banusername";
    public override string Help => Loc.GetString("cmd-ban-username-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 3)
        {
            shell.WriteError(LocalizationManager.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 3)));
            return;
        }

        var issuer = shell.Player?.UserId;
        string username = args[0];
        string message;
        bool ban = false;

        // DANGER!: if UsernameHelpers changes it may permit username strings which are stronger versions of regex than single match
        if (!UsernameHelpers.IsNameValid(username, out var reason))
        {
            shell.WriteError(Loc.GetString("cmd-ban-username-invalid-username", ("reason", reason)));
            return;
        }

        if (args.Length > 1)
        {
            message = args[1];
        }
        else
        {
            message = username;
        }

        if (args.Length > 2 && !bool.TryParse(args[2], out ban))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-boolean"));
            return;
        }

        _usernameRules.CreateUsernameRule(false, username, message, issuer, ban);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("shell-argument-username-hint"));
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
        }

        return CompletionResult.Empty;
    }
}
