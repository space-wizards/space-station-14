using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleUnbanCommand : LocalizedCommands
{
    [Dependency] private readonly IBanManager _banManager = default!;

    public override string Command => "roleunban";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var banId))
        {
            shell.WriteLine(Loc.GetString($"cmd-roleunban-unable-to-parse-id", ("id", args[0]), ("help", Help)));
            return;
        }

        var response = await _banManager.PardonRoleBan(banId, shell.Player?.UserId, DateTimeOffset.Now);
        shell.WriteLine(response);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // Can't think of good way to do hint options for this
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-roleunban-hint-1")),
            _ => CompletionResult.Empty
        };
    }
}
