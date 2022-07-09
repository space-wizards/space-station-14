using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetAtmosTemperatureCommand : IConsoleCommand
    {
        public string Command => "setatmostemp";
        public string Description => "Sets a grid's temperature (in kelvin).";
        public string Help => "Usage: setatmostemp <GridId> <Temperature>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2) return;
            if(!EntityUid.TryParse(args[0], out var gridId)
               || !float.TryParse(args[1], out var temperature)) return;

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine("Invalid temperature.");
                return;
            }

            if (!gridId.IsValid() || !mapMan.TryGetGrid(gridId, out var gridComp))
            {
                shell.WriteLine("Invalid grid ID.");
                return;
            }

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var tiles = 0;
            foreach (var tile in atmosphereSystem.GetAllMixtures(gridComp.GridEntityId, true))
            {
                tiles++;
                tile.Temperature = temperature;
            }

            shell.WriteLine($"Changed the temperature of {tiles} tiles.");
        }
    }
}
