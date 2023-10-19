using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetTemperatureCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "settemp";
        public string Description => "Sets a tile's temperature (in kelvin).";
        public string Help => "Usage: settemp <X> <Y> <GridId> <Temperature>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4)
                return;

            if (!int.TryParse(args[0], out var x)
                || !int.TryParse(args[1], out var y)
                || !NetEntity.TryParse(args[2], out var gridIdNet)
                || !_entities.TryGetEntity(gridIdNet, out var gridId)
                || !float.TryParse(args[3], out var temperature))
            {
                return;
            }

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine("Invalid temperature.");
                return;
            }

            if (!_entities.HasComponent<MapGridComponent>(gridId))
            {
                shell.WriteError("Invalid grid.");
                return;
            }

            var atmospheres = _entities.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            var indices = new Vector2i(x, y);

            var tile = atmospheres.GetTileMixture(gridId, null, indices, true);

            if (tile == null)
            {
                shell.WriteLine("Invalid coordinates or tile.");
                return;
            }

            tile.Temperature = temperature;
        }
    }
}
