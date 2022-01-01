using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public class AddWhitelistCommand : IConsoleCommand
{
    public string Command => "whitelistadd";
    public string Description => "a";
    public string Help => "a";
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
            var guid = data.UserId.UserId;
            var isWhitelisted = await db.GetWhitelistStatusAsync(guid);
            if (isWhitelisted)
                return;

            await db.AddToWhitelistAsync(guid);
        }
    }
}

[AdminCommand(AdminFlags.Ban)]
public class RemoveWhitelistCommand : IConsoleCommand
{
    public string Command => "whitelistremove";
    public string Description => "a";
    public string Help => "a";
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
            var guid = data.UserId.UserId;
            var isWhitelisted = await db.GetWhitelistStatusAsync(guid);
            if (!isWhitelisted)
                return;

            await db.RemoveFromWhitelistAsync(guid);
        }
    }
}
