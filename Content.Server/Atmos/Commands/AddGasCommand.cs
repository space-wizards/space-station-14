using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class AddGasCommand : IConsoleCommand
    {
        public string Command => "addgas";
        public string Description => "Adds gas at a certain position.";
        public string Help => "addgas <X> <Y> <GridId> <Gas> <moles>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 5) return;
            if(!int.TryParse(args[0], out var x)
               || !int.TryParse(args[1], out var y)
               || !int.TryParse(args[2], out var id)
               || !(AtmosCommandUtils.TryParseGasID(args[3], out var gasId))
               || !float.TryParse(args[4], out var moles)) return;

            var gridId = new GridId(id);

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            var indices = new Vector2i(x, y);
            var tile = atmosphereSystem.GetTileMixture(gridId, indices, true);

            if (tile == null)
            {
                shell.WriteLine("Invalid coordinates or tile.");
                return;
            }

            tile.AdjustMoles(gasId, moles);
        }
    }
}
