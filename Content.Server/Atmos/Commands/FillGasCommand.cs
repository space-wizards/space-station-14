using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class FillGas : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public string Command => "fillgas";
        public string Description => "Adds gas to all tiles in a grid.";
        public string Help => "fillgas <GridEid> <Gas> <moles>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
                return;

            if (!NetEntity.TryParse(args[0], out var gridIdNet)
                || !_entManager.TryGetEntity(gridIdNet, out var gridId)
                || !(AtmosCommandUtils.TryParseGasID(args[1], out var gasId))
                || !float.TryParse(args[2], out var moles))
            {
                return;
            }

            if (!_mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteLine("Invalid grid ID.");
                return;
            }

            var atmosphereSystem = _entManager.System<AtmosphereSystem>();

            foreach (var tile in atmosphereSystem.GetAllMixtures(grid.Owner, true))
            {
                tile.AdjustMoles(gasId, moles);
            }
        }
    }

}
