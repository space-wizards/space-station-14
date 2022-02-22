using Content.Server.Station;
using Content.Shared.Administration;
using Content.Shared.Station;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Admin)]
public sealed class ListStationJobsCommand : IConsoleCommand
{
    public string Command => "lsstationjobs";

    public string Description => "Lists all jobs on the given station.";

    public string Help => "lsstationjobs <station id>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
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

        foreach (var (job, amount) in stationSystem.StationInfo[new StationId(station)].JobList)
        {
            shell.WriteLine($"{job}: {amount}");
        }
    }
}
