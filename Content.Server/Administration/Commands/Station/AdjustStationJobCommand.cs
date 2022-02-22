using Content.Server.Station;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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


        var stationSystem = EntitySystem.Get<StationSystem>();

        if (!uint.TryParse(args[0], out var station) || !stationSystem.StationInfo.ContainsKey(new StationId(station)))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

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

        stationSystem.AdjustJobsAvailableOnStation(new StationId(station), job, amount);
    }
}
