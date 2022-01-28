using Content.Server.Station;
using Content.Shared.Administration;
using Content.Shared.Station;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Admin)]
public class RenameStationCommand : IConsoleCommand
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

        if (!uint.TryParse(args[0], out var station) || !stationSystem.StationInfo.ContainsKey(new StationId(station)))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        stationSystem.RenameStation(new StationId(station), args[1]);
    }
}
