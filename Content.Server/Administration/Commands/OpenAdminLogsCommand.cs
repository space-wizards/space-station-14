using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed partial class OpenAdminLogsCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _adminLogManager = default!;
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private IPlayerManager _players = default!;

    public override string Command => Cmd;
    public const string Cmd = "adminlogs";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        Guid? player = null;

        switch (args.Length)
        {
            case 1 when Guid.TryParse(args[0], out var playerGuid):
                player = playerGuid;
                break;
            case 1:
                var dbGuid = await _locator.LookupIdByNameAsync(args[0]);

                if (dbGuid == null)
                {
                    shell.WriteError(Loc.GetString("cmd-admin-logs-wrong-target", ("user", args[0])));
                    return;
                }

                player = dbGuid.UserId;
                break;
        }
        _adminLogManager.OpenEui(admin, targetPlayer: player);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _players.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-admin-logs-hint"));
    }
}
