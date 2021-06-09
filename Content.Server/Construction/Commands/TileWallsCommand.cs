using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Mapping)]
    class TileWallsCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "tilewalls";
        public string Description => "Puts an underplating tile below every wall on a grid.";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            GridId gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity == null)
                    {
                        shell.WriteLine("Only a player can run this command.");
                        return;
                    }

                    gridId = player.AttachedEntity.Transform.GridID;
                    break;
                case 1:
                    if (!int.TryParse(args[0], out var id))
                    {
                        shell.WriteLine($"{args[0]} is not a valid integer.");
                        return;
                    }

                    gridId = new GridId(id);
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteLine($"No grid exists with id {gridId}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                shell.WriteLine($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var underplating = tileDefinitionManager["underplating"];
            var underplatingTile = new Tile(underplating.TileId);
            var changed = 0;
            foreach (var childUid in gridEntity.Transform.ChildEntityUids)
            {
                if (!entityManager.TryGetEntity(childUid, out var childEntity))
                {
                    continue;
                }

                var prototype = childEntity.Prototype;
                while (true)
                {
                    if (prototype?.Parent == null)
                    {
                        break;
                    }

                    prototype = prototypeManager.Index<EntityPrototype>(prototype.Parent);
                }

                if (prototype?.ID != "base_wall")
                {
                    continue;
                }

                if (!childEntity.Transform.Anchored)
                {
                    continue;
                }

                var tile = grid.GetTileRef(childEntity.Transform.Coordinates);
                var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];

                if (tileDef.Name == "underplating")
                {
                    continue;
                }

                grid.SetTile(childEntity.Transform.Coordinates, underplatingTile);
                changed++;
            }

            shell.WriteLine($"Changed {changed} tiles.");
        }
    }
}
