using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetTemperatureCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

        public override string Command => "settemp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4)
                return;

            if (!int.TryParse(args[0], out var x)
                || !int.TryParse(args[1], out var y)
                || !NetEntity.TryParse(args[2], out var gridIdNet)
                || !EntityManager.TryGetEntity(gridIdNet, out var gridId)
                || !float.TryParse(args[3], out var temperature))
            {
                return;
            }

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine(Loc.GetString($"cmd-setatmostemp-invalid-temperature"));
                return;
            }

            if (!EntityManager.HasComponent<MapGridComponent>(gridId))
            {
                shell.WriteError(Loc.GetString($"shell-invalid-grid-id-specific", ("grid", gridId)));
                return;
            }

            var indices = new Vector2i(x, y);
            var tile = _atmosSystem.GetTileMixture(gridId, null, indices, true);

            if (tile == null)
            {
                shell.WriteLine(Loc.GetString($"cmd-addgas-invalid-coordinates"));
                return;
            }

            tile.Temperature = temperature;
        }
    }
}
