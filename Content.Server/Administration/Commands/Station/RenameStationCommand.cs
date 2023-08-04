using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Admin)]
public sealed class RenameStationCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entSysManager = default!;

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

        var stationSystem = _entSysManager.GetEntitySystem<StationSystem>();

        if (!EntityUid.TryParse(args[0], out var station) || _entityManager.HasComponent<StationDataComponent>(station))
        {
            shell.WriteError(Loc.GetString("shell-argument-station-id-invalid", ("index", 1)));
            return;
        }

        stationSystem.RenameStation(station, args[1]);
    }
}
