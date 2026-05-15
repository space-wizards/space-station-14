using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetAtmosTemperatureCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override string Command => "setatmostemp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
                return;

            if (!EntityManager.TryParseNetEntity(args[0], out var gridId)
                || !float.TryParse(args[1], out var temperature))
            {
                return;
            }

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine(Loc.GetString("cmd-setatmostemp-invalid-temperature"));
                return;
            }

            if (!gridId.Value.IsValid() || !EntityManager.HasComponent<MapGridComponent>(gridId))
            {
                shell.WriteLine(Loc.GetString("cmd-setatmostemp-invalid-grid"));
                return;
            }

            var tiles = 0;
            foreach (var tile in _atmosphereSystem.GetAllMixtures(gridId.Value, true))
            {
                tiles++;
                tile.Temperature = temperature;
            }

            shell.WriteLine(Loc.GetString("cmd-setatmostemp-changed-temperature", ("tiles", tiles)));
        }
    }
}
