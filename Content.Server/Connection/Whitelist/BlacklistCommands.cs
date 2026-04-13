using Content.Server.Administration;
using Content.Server.Administration.AuditLog;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.Connection.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public sealed class AddBlacklistCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override string Command => "blacklistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = args[0];
        var data = await _playerLocator.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-blacklistadd-not-found", ("username", args[0])));
            return;
        }
        var guid = data.UserId;
        var isBlacklisted = await _db.GetBlacklistStatusAsync(guid);
        if (isBlacklisted)
        {
            shell.WriteLine(Loc.GetString("cmd-blacklistadd-existing", ("username", data.Username)));
            return;
        }

        await _db.AddToBlacklistAsync(guid);

        if (shell.Player != null)
        {
            _auditLog.LogAction(
                shell.Player.UserId.UserId,
                AdminAuditAction.BlacklistAdd,
                AuditSeverity.Notable,
                $"Added {data.Username} to blacklist",
                targetPlayerUserId: guid.UserId);
        }

        shell.WriteLine(Loc.GetString("cmd-blacklistadd-added", ("username", data.Username)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-blacklistadd-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveBlacklistCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override string Command => "blacklistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = args[0];
        var data = await _playerLocator.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-blacklistremove-not-found", ("username", args[0])));
            return;
        }

        var guid = data.UserId;
        var isBlacklisted = await _db.GetBlacklistStatusAsync(guid);
        if (!isBlacklisted)
        {
            shell.WriteLine(Loc.GetString("cmd-blacklistremove-existing", ("username", data.Username)));
            return;
        }

        await _db.RemoveFromBlacklistAsync(guid);

        if (shell.Player != null)
        {
            _auditLog.LogAction(
                shell.Player.UserId.UserId,
                AdminAuditAction.BlacklistRemove,
                AuditSeverity.Notable,
                $"Removed {data.Username} from blacklist",
                targetPlayerUserId: guid.UserId);
        }

        shell.WriteLine(Loc.GetString("cmd-blacklistremove-removed", ("username", data.Username)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-blacklistremove-arg-player"));
        }

        return CompletionResult.Empty;
    }
}
