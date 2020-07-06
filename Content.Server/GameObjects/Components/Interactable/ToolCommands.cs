#nullable enable
using Content.Shared.Maps;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    /// <see cref="TilePryingComponent.TryPryTile"/>
    /// </summary>
    class TilePryCommand : IClientCommand
    {
        public string Command => "tilepry";
        public string Description => "Pries up all tiles in a radius around the user.";
        public string Help => $"Usage: {Command} <radius>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player?.AttachedEntity == null)
            {
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, Help);
                return;
            }

            if (!int.TryParse(args[0], out var radius))
            {
                shell.SendText(player, $"{args[0]} isn't a valid integer.");
                return;
            }

            if (radius < 0)
            {
                shell.SendText(player, "Radius must be positive.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var playerGrid = player.AttachedEntity.Transform.GridID;
            var mapGrid = mapManager.GetGrid(playerGrid);
            var playerPosition = player.AttachedEntity.Transform.GridPosition;
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            for (var i = -radius; i <= radius; i++)
            {
                for (var j = -radius; j <= radius; j++)
                {
                    var tile = mapGrid.GetTileRef(playerPosition.Offset((i, j)));
                    var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
                    var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];

                    if (!tileDef.CanCrowbar) continue;

                    var underplating = tileDefinitionManager["underplating"];
                    mapGrid.SetTile(coordinates, new Tile(underplating.TileId));
                }
            }
        }
    }

    class AnchorCommand : IClientCommand
    {
        public string Command => "anchor";
        public string Description => "Anchors all entities in a radius around the user";
        public string Help => $"Usage: {Command} <radius>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player?.AttachedEntity == null)
            {
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, Help);
                return;
            }

            if (!int.TryParse(args[0], out var radius))
            {
                shell.SendText(player, $"{args[0]} isn't a valid integer.");
                return;
            }

            if (radius < 0)
            {
                shell.SendText(player, "Radius must be positive.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(player.AttachedEntity.Transform.GridID, out var grid))
            {
                shell.SendText(player, "You are not on a valid grid.");
                return;
            }

            var snapGrids = grid.GetSnapGridCell(player.AttachedEntity.Transform.GridPosition, SnapGridOffset.Center);

            foreach (var snapGrid in snapGrids)
            {
                foreach (var cell in snapGrid.GetCellsInSquareArea(radius))
                {
                    foreach (var entity in cell.GetLocal())
                    {
                        if (entity.TryGetComponent(out AnchorableComponent anchorable))
                        {
                            anchorable.TryAnchor(player.AttachedEntity, force: true);
                        }
                    }
                }
            }
        }
    }

    class UnAnchorCommand : IClientCommand
    {
        public string Command => "unanchor";
        public string Description => "Unanchors all anchorable entities in a radius around the user";
        public string Help => $"Usage: {Command} <radius>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player?.AttachedEntity == null)
            {
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, Help);
                return;
            }

            if (!int.TryParse(args[0], out var radius))
            {
                shell.SendText(player, $"{args[0]} isn't a valid integer.");
                return;
            }

            if (radius < 0)
            {
                shell.SendText(player, "Radius must be positive.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(player.AttachedEntity.Transform.GridID, out var grid))
            {
                shell.SendText(player, "You are not on a valid grid.");
                return;
            }

            var snapGrids = grid.GetSnapGridCell(player.AttachedEntity.Transform.GridPosition, SnapGridOffset.Center);

            foreach (var snapGrid in snapGrids)
            {
                foreach (var cell in snapGrid.GetCellsInSquareArea(radius))
                {
                    foreach (var entity in cell.GetLocal())
                    {
                        if (entity.TryGetComponent(out AnchorableComponent anchorable))
                        {
                            anchorable.TryUnAnchor(player.AttachedEntity, force: true);
                        }
                    }
                }
            }
        }
    }
}
