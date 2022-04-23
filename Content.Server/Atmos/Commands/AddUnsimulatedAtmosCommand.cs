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
    public sealed class AddUnsimulatedAtmosCommand : IConsoleCommand
    {
        public string Command => "addunsimulatedatmos";
        public string Description => "Adds unimulated atmos support to a grid.";
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

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.EntityExists(gridComp.GridEntityId))
            {
                shell.WriteLine("Failed to get grid entity.");
                return;
            }

            if (entMan.HasComponent<IAtmosphereComponent>(gridComp.GridEntityId))
            {
                shell.WriteLine("Grid already has an atmosphere.");
                return;
            }

            entMan.AddComponent<UnsimulatedGridAtmosphereComponent>(gridComp.GridEntityId);

            shell.WriteLine($"Added unsimulated atmosphere to grid {id}.");
        }
    }

}
