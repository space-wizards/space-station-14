using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Players.Whitelist;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public sealed partial class AddWhitelistCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private WhitelistManager _whitelistManager = default!;
    public override string Command => "whitelistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            if (_whitelistManager.IsWhitelisted(guid))
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistadd-existing", ("username", data.Username)));
                return;
            }

            _whitelistManager.AddWhitelist(guid);
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

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed partial class RemoveWhitelistCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private WhitelistManager _whitelistManager = default!;

    public override string Command => "whitelistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            if (!_whitelistManager.IsWhitelisted(guid))
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistremove-existing", ("username", data.Username)));
                return;
            }

            _whitelistManager.RemoveWhitelist(guid);
            shell.WriteLine(Loc.GetString("cmd-whitelistremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistremove-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistremove-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed partial class KickNonWhitelistedCommand : LocalizedCommands
{
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerDbManager _dbManager = default!;

    public override string Command => "kicknonwhitelisted";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 0), ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        if (!_configManager.GetCVar(CCVars.WhitelistEnabled))
            return;

        foreach (var session in _playerManager.NetworkedSessions)
        {
            if (await _dbManager.GetAdminDataForAsync(session.UserId) is not null)
                continue;

            // We let this one query the whitelist to be 100% certain it's kicking out non-whitelisted players +
            // it's mostly adding/removing whitelists that needs to go through the cache.
            if (!await _dbManager.GetWhitelistStatusAsync(session.UserId))
                _netManager.DisconnectChannel(session.Channel, Loc.GetString("whitelist-not-whitelisted"));
        }
    }
}
