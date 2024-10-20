using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public sealed class AddWhitelistCommand : LocalizedCommands
{
    public override string Command => "whitelistadd";

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _loc = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 2)));
            shell.WriteLine(Help);
            return;
        }

        var name = args[0];
        var whitelistName = args.Length == 2 ? args[1] : _config.GetCVar(CCVars.ActiveWhitelist);

        var data = await _loc.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _db.GetWhitelistStatusAsync(guid, whitelistName);

            if (isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistadd-existing", ("username", data.Username)));
                return;
            }

            if (!await _db.AddToWhitelistAsync(guid, whitelistName))
            {
                shell.WriteError(Loc.GetString("cmd-whitelistadd-adding-failed"));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-whitelistadd-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistadd-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistadd-arg-player"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistadd-arg-whitelist-name"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _loc = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    public override string Command => "whitelistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 1), ("upper", 2)));
            shell.WriteLine(Help);
            return;
        }

        var name = args[0];
        var whitelistName = args.Length == 2 ? args[1] : _config.GetCVar(CCVars.ActiveWhitelist);

        var data = await _loc.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _db.GetWhitelistStatusAsync(guid, whitelistName);

            if (!isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistremove-existing", ("username", data.Username)));
                return;
            }

            if(!await _db.RemoveFromWhitelistAsync(guid, whitelistName))
            {
                shell.WriteError(Loc.GetString("cmd-whitelistremove-adding-failed"));
                return;

            }
            shell.WriteLine(Loc.GetString("cmd-whitelistremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistremove-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistremove-arg-player"));


        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistsremove-arg-whitelist-name"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
    public sealed class ListWhitelissCommand : LocalizedCommands
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerLocator _loc = default!;

        public override string Command => "listwhitelists";

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 1), ("currentAmount", args.Length)));
                shell.WriteLine(Help);
                return;
            }

            var player = await _loc.LookupIdByNameOrIdAsync(args[0]);

            if (player == null)
            {
                shell.WriteError(Loc.GetString("cmd-listwhitelists-player-not-found", ("player", args[0])));
                return;
            }

            var whitelists = string.Join(", ", await _db.GetPlayerWhitelistsAsync(player.UserId));

           shell.WriteLine(Loc.GetString("cmd-listwhitelists-result", ("username", args[0]), ("whitelists", whitelists)));

        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint(Loc.GetString("cmd-listwhitelists-arg-player"));

            return CompletionResult.Empty;
        }
    }

[AdminCommand(AdminFlags.Ban)]
public sealed class KickNonWhitelistedCommand : LocalizedCommands
{
    public override string Command => "kicknonwhitelisted";

    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IServerNetManager _net = default!;

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 0), ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        if (!_config.GetCVar(CCVars.WhitelistEnabled))
            return;

        var active = _config.GetCVar(CCVars.ActiveWhitelist);

        foreach (var session in _player.NetworkedSessions)
        {
            if (await _db.GetAdminDataForAsync(session.UserId) is not null)
                continue;

            if (!await _db.GetWhitelistStatusAsync(session.UserId, active))
            {
                _net.DisconnectChannel(session.Channel, Loc.GetString("whitelist-not-whitelisted"));
            }
        }
    }
}
