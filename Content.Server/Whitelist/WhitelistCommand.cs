using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Toolshed;

namespace Content.Server.Whitelist;

[ToolshedCommand, AdminCommand(AdminFlags.Ban)]
public sealed class WhitelistCommand : ToolshedCommand
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    [CommandImplementation("add")]
    public async void AddWhitelist(IInvocationContext ctx, string name)
    {
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _dbManager.GetWhitelistStatusAsync(guid);
            if (isWhitelisted)
            {
                ctx.WriteLine(Loc.GetString("cmd-whitelistadd-existing", ("username", data.Username)));
                return;
            }

            await _dbManager.AddToWhitelistAsync(guid);
            ctx.WriteLine(Loc.GetString("cmd-whitelistadd-added", ("username", data.Username)));
            return;
        }

        ctx.WriteLine(Loc.GetString("command-whitelist-not-found", ("username", name)));
    }

    [CommandImplementation("remove")]
    public async void RemoveWhitelist(IInvocationContext ctx, string name)
    {
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _dbManager.GetWhitelistStatusAsync(guid);
            if (!isWhitelisted)
            {
                ctx.WriteLine(Loc.GetString("cmd-whitelistremove-existing", ("username", data.Username)));
                return;
            }

            await _dbManager.RemoveFromWhitelistAsync(guid);
            ctx.WriteLine(Loc.GetString("cmd-whitelistremove-removed", ("username", data.Username)));
            return;
        }

        ctx.WriteLine(Loc.GetString("command-whitelist-not-found", ("username", name)));
    }

    [CommandImplementation("kickNonWhitelisted")]
    public async void KickNonWhitelisted()
    {
        if (!_configManager.GetCVar(CCVars.WhitelistEnabled))
            return;

        foreach (var session in _playerManager.NetworkedSessions)
        {
            if (await _dbManager.GetAdminDataForAsync(session.UserId) is not null)
                continue;

            if (!await _dbManager.GetWhitelistStatusAsync(session.UserId))
                _netManager.DisconnectChannel(session.Channel, Loc.GetString("whitelist-manual"));
        }
    }
}
