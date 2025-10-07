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
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override string Command => "setatmostemp";

        public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
                return;

            if (!_entManager.TryParseNetEntity(args[0], out var gridId)
                || !float.TryParse(args[1], out var temperature))
            {
                return;
            }

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-invalid-temperature"));
                return;
            }

            if (!gridId.Value.IsValid() || !_entManager.HasComponent<MapGridComponent>(gridId))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-invalid-grid"));
                return;
            }

            var tiles = 0;
            foreach (var tile in _atmosphereSystem.GetAllMixtures(gridId.Value, true))
            {
                tiles++;
                tile.Temperature = temperature;
            }

            shell.WriteLine(Loc.GetString($"cmd-{Command}-changed-temperature", ("tiles", tiles)));
        }
    }
}
