using Content.Server.GameObjects.Components.Atmos;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public class AddAtmos : IClientCommand
    {
        public string Command => "addatmos";
        public string Description => "Adds atmos support to a grid.";
        public string Help => "addatmos <GridId>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length < 1) return;
            if(!int.TryParse(args[0], out var id)) return;

            var gridId = new GridId(id);

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (!gridId.IsValid() || !mapMan.TryGetGrid(gridId, out var gridComp))
            {
                shell.SendText(player, "Invalid grid ID.");
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetEntity(gridComp.GridEntityId, out var grid))
            {
                shell.SendText(player, "Failed to get grid entity.");
                return;
            }

            if (grid.HasComponent<GridAtmosphereComponent>())
            {
                shell.SendText(player, "Grid already has an atmosphere.");
                return;
            }

            grid.AddComponent<GridAtmosphereComponent>();

            shell.SendText(player, $"Added atmosphere to grid {id}.");
        }
    }
}
