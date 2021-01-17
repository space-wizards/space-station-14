#nullable enable
using Content.Server.Administration;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class AddAtmosCommand : IClientCommand
    {
        public string Command => "addatmos";
        public string Description => "Adds atmos support to a grid.";
        public string Help => $"{Command} <GridId>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length < 1)
            {
                shell.SendText(player, Help);
                return;
            }

            if (!int.TryParse(args[0], out var id))
            {
                shell.SendText(player, $"{args[0]} is not a valid integer.");
                return;
            }

            var gridId = new GridId(id);

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (!gridId.IsValid() || !mapMan.TryGetGrid(gridId, out var gridComp))
            {
                shell.SendText(player, $"{gridId} is not a valid grid id.");
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetEntity(gridComp.GridEntityId, out var grid))
            {
                shell.SendText(player, "Failed to get grid entity.");
                return;
            }

            if (grid.HasComponent<IGridAtmosphereComponent>())
            {
                shell.SendText(player, "Grid already has an atmosphere.");
                return;
            }

            grid.AddComponent<GridAtmosphereComponent>();

            shell.SendText(player, $"Added atmosphere to grid {id}.");
        }
    }
}
