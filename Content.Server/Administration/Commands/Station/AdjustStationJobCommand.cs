using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Round)]
public sealed class AdjustStationJobCommand : IConsoleCommand
{
    public string Command => "adjstationjob";

    public string Description => "Adjust the job manifest on a station.";

    public string Help => "adjstationjob <station id> <job id> <amount>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var stationSystem = EntitySystem.Get<StationSystem>();
        var stationJobs = EntitySystem.Get<StationJobsSystem>();

        if (!int.TryParse(args[0], out var stationInt) || !stationSystem.Stations.Contains(new EntityUid(stationInt)))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        var station = new EntityUid(stationInt);

        if (!prototypeManager.TryIndex<JobPrototype>(args[1], out var job))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-prototype",
                ("index", 2), ("prototypeName", nameof(JobPrototype))));
            return;
        }

        if (!int.TryParse(args[2], out var amount) || amount < -1)
        {
            shell.WriteError(Loc.GetString("shell-argument-number-must-be-between",
                ("index", 3), ("lower", -1), ("upper", int.MaxValue)));
            return;
        }

        if (amount == -1)
        {
            stationJobs.MakeJobUnlimited(station, job);
            return;
        }

        stationJobs.TrySetJobSlot(station, job, amount, true);
    }
}
