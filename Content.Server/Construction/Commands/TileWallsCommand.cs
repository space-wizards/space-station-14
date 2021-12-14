using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Commands
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
            var entityManager = IoCManager.Resolve<IEntityManager>();
            GridId gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteLine("Only a player can run this command.");
                        return;
                    }

                    gridId = entityManager.GetComponent<TransformComponent>(playerEntity).GridID;
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

            if (!entityManager.EntityExists(grid.GridEntityId))
            {
                shell.WriteLine($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var underplating = tileDefinitionManager["underplating"];
            var underplatingTile = new Tile(underplating.TileId);
            var changed = 0;
            foreach (var child in entityManager.GetComponent<TransformComponent>(grid.GridEntityId).ChildEntities)
            {
                if (!entityManager.EntityExists(child))
                {
                    continue;
                }

                var prototype = entityManager.GetComponent<MetaDataComponent>(child).EntityPrototype;
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

                var childTransform = entityManager.GetComponent<TransformComponent>(child);

                if (!childTransform.Anchored)
                {
                    continue;
                }

                var tile = grid.GetTileRef(childTransform.Coordinates);
                var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];

                if (tileDef.Name == "underplating")
                {
                    continue;
                }

                grid.SetTile(childTransform.Coordinates, underplatingTile);
                changed++;
            }

            shell.WriteLine($"Changed {changed} tiles.");
        }
    }
}
