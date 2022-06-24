using System.Net;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class ImportNetsetCommand : IConsoleCommand
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IServerDbManager _serverDbManager = default!;

    public static NetUserId AutomationBanningUser = new(new Guid("a17aa146-807c-48bb-8573-65e3cb05119d"));

    public string Command => "importnetset";
    public string Description => "Import a .netset file into the ban database.";
    public string Help => "importnetset <filepath in vfs> \"<ban reason>\"";
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var stream = _resourceManager.ContentFileReadText(new ResourcePath(args[0]).ToRootedPath());

        var bansToAdd = new List<ServerBanDef>();
        var line = await stream.ReadLineAsync();
        while (line != null)
        {
            if (line.StartsWith("#")) // That's a comment, skip it.
            {
                line = await stream.ReadLineAsync();
                continue;
            }

            if (bansToAdd.Count % 100 == 0)
                shell.WriteLine($"Got {bansToAdd.Count} bans so far...");

            var ipMaskSplit = line.Split("/");

            if (ipMaskSplit.Length > 2)
            {
                shell.WriteError($"Failed to import address {line}");
                return;
            }

            if (!IPAddress.TryParse(ipMaskSplit[0], out var ip))
            {
                shell.WriteError($"Failed to import address {line}");
                return;
            }

            var cidr = 32;
            if (ipMaskSplit.Length == 2)
            {
                if ( !int.TryParse(ipMaskSplit[1], out cidr))
                {
                    shell.WriteError($"Failed to import address {line}");
                    return;
                }
            }

            var possibleCollisions = await _serverDbManager.GetServerBansAsync(ip, null, null, false);
            var collides = false;
            foreach (var ban in possibleCollisions)
            {
                if (ban.UserId != null || ban.ExpirationTime != null || ban.HWId == null)
                    continue;

                if (ban.Address == null) // no clue how we'd get here but best be safe.
                    continue;

                if (ban.Address.Value.cidrMask > cidr)
                    continue;

                collides = true;
                shell.WriteError($"The imported address {ip}/{cidr} collides with {ban.Address.Value.address}/{ban.Address.Value.cidrMask}, ban {ban.Id}. Add it manually.");
            }

            if (collides)
            {
                line = await stream.ReadLineAsync();
                continue; // not gonna add a ban that collides.
            }

            var def = new ServerBanDef(
                null,
                null,
                (ip, cidr),
                null,
                DateTimeOffset.UnixEpoch,
                null,
                args[1],
                AutomationBanningUser,
                null);

            bansToAdd.Add(def);

            line = await stream.ReadLineAsync();
        }

        shell.WriteLine($"Importing {bansToAdd.Count} bans");

        foreach (var ban in bansToAdd)
        {
            await _serverDbManager.AddServerBanAsync(ban);
        }
        shell.WriteLine("Import complete!");
    }
}
