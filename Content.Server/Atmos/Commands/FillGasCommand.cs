using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class FillGas : IConsoleCommand
    {
        public string Command => "fillgas";
        public string Description => "Adds gas to all tiles in a grid.";
        public string Help => "fillgas <GridId> <Gas> <moles>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3) return;
            if(!int.TryParse(args[0], out var id)
               || !(AtmosCommandUtils.TryParseGasID(args[1], out var gasId))
               || !float.TryParse(args[2], out var moles)) return;

            var gridId = new GridId(id);

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (!gridId.IsValid() || !mapMan.TryGetGrid(gridId, out _))
            {
                shell.WriteLine("Invalid grid ID.");
                return;
            }

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            foreach (var tile in atmosphereSystem.GetAllTileMixtures(gridId, true))
            {
                tile.AdjustMoles(gasId, moles);
            }
        }
    }

}
