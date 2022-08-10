using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

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
        var stationJobs = EntitySystem.Get<StationJobsSystem>();

        if (!int.TryParse(args[0], out var station) || !stationSystem.Stations.Contains(new EntityUid(station)))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        foreach (var (job, amount) in stationJobs.GetJobs(new EntityUid(station)))
        {
            var amountText = amount is null ? "Infinite" : amount.ToString();
            shell.WriteLine($"{job}: {amountText}");
        }
    }
}
