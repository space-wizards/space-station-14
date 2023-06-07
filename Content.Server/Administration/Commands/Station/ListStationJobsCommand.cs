using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Admin)]
public sealed class ListStationJobsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entSysManager = default!;

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

        var stationSystem = _entSysManager.GetEntitySystem<StationSystem>();
        var stationJobs = _entSysManager.GetEntitySystem<StationJobsSystem>();

        if (!EntityUid.TryParse(args[0], out var station) || !_entityManager.HasComponent<StationJobsComponent>(station))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        foreach (var (job, amount) in stationJobs.GetJobs(station))
        {
            var amountText = amount is null ? "Infinite" : amount.ToString();
            shell.WriteLine($"{job}: {amountText}");
        }
    }
}
