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
public sealed class AddWhitelistCommand : IConsoleCommand
{
    public string Command => "whitelistadd";
    public string Description => Loc.GetString("command-whitelistadd-description");
    public string Help => Loc.GetString("command-whitelistadd-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await db.GetWhitelistStatusAsync(guid);
            if (isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("command-whitelistadd-existing", ("username", data.Username)));
                return;
            }

            await db.AddToWhitelistAsync(guid);
            shell.WriteLine(Loc.GetString("command-whitelistadd-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("command-whitelistadd-not-found", ("username", args[0])));
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveWhitelistCommand : IConsoleCommand
{
    public string Command => "whitelistremove";
    public string Description => Loc.GetString("command-whitelistremove-description");
    public string Help => Loc.GetString("command-whitelistremove-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await db.GetWhitelistStatusAsync(guid);
            if (!isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("command-whitelistremove-existing", ("username", data.Username)));
                return;
            }

            await db.RemoveFromWhitelistAsync(guid);
            shell.WriteLine(Loc.GetString("command-whitelistremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("command-whitelistremove-not-found", ("username", args[0])));
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class KickNonWhitelistedCommand : IConsoleCommand
{
    public string Command => "kicknonwhitelisted";
    public string Description => Loc.GetString("command-kicknonwhitelisted-description");
    public string Help => Loc.GetString("command-kicknonwhitelisted-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
            return;

        var cfg = IoCManager.Resolve<IConfigurationManager>();

        if (!cfg.GetCVar(CCVars.WhitelistEnabled))
            return;

        var player = IoCManager.Resolve<IPlayerManager>();
        var db = IoCManager.Resolve<IServerDbManager>();
        var net = IoCManager.Resolve<IServerNetManager>();

        foreach (var session in player.NetworkedSessions)
        {
            if (await db.GetAdminDataForAsync(session.UserId) is not null)
                continue;

            if (!await db.GetWhitelistStatusAsync(session.UserId))
            {
                net.DisconnectChannel(session.ConnectedClient, Loc.GetString("whitelist-not-whitelisted"));
            }
        }

    }
}
