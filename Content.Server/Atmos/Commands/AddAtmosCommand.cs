using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class AddAtmosCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "addatmos";
        public string Description => "Adds atmos support to a grid.";
        public string Help => $"{Command} <GridId>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.WriteLine($"{args[0]} is not a valid integer.");
                return;
            }

            var gridId = new GridId(id);

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (!gridId.IsValid() || !mapMan.TryGetGrid(gridId, out var gridComp))
            {
                shell.WriteLine($"{gridId} is not a valid grid id.");
                return;
            }

            if (!_entities.EntityExists(gridComp.GridEntityId))
            {
                shell.WriteLine("Failed to get grid entity.");
                return;
            }

            if (_entities.HasComponent<IAtmosphereComponent>(gridComp.GridEntityId))
            {
                shell.WriteLine("Grid already has an atmosphere.");
                return;
            }

            _entities.AddComponent<GridAtmosphereComponent>(gridComp.GridEntityId);

            shell.WriteLine($"Added atmosphere to grid {id}.");
        }
    }
}
