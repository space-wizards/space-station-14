using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Admin)]
public sealed class RenameStationCommand : IConsoleCommand
{
    public string Command => "renamestation";

    public string Description => "Renames the given station";

    public string Help => "renamestation <station id> <name>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var stationSystem = EntitySystem.Get<StationSystem>();

        if (!int.TryParse(args[0], out var station) || !stationSystem.Stations.Contains(new EntityUid(station)))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        stationSystem.RenameStation(new EntityUid(station), args[1]);
    }
}
