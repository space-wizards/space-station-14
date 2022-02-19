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
    public sealed class SetTemperatureCommand : IConsoleCommand
    {
        public string Command => "settemp";
        public string Description => "Sets a tile's temperature (in kelvin).";
        public string Help => "Usage: settemp <X> <Y> <GridId> <Temperature>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4) return;
            if(!int.TryParse(args[0], out var x)
               || !int.TryParse(args[1], out var y)
               || !int.TryParse(args[2], out var id)
               || !float.TryParse(args[3], out var temperature)) return;

            var gridId = new GridId(id);

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine("Invalid temperature.");
                return;
            }

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            var indices = new Vector2i(x, y);
            var tile = atmosphereSystem.GetTileMixture(gridId, indices, true);

            if (tile == null)
            {
                shell.WriteLine("Invalid coordinates or tile.");
                return;
            }

            tile.Temperature = temperature;
        }
    }
}
