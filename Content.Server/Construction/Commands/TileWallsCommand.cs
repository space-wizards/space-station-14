using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Construction.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    sealed class TileWallsCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "tilewalls";
        public string Description => "Puts an underplating tile below every wall on a grid.";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        public const string TilePrototypeId = "Plating";
        public const string WallTag = "Wall";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var entityManager = IoCManager.Resolve<IEntityManager>();
            EntityUid? gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteLine("Only a player can run this command.");
                        return;
                    }

                    gridId = entityManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                    break;
                case 1:
                    if (!EntityUid.TryParse(args[0], out var id))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity.");
                        return;
                    }

                    gridId = id;
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
            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();
            var underplating = tileDefinitionManager[TilePrototypeId];
            var underplatingTile = new Tile(underplating.TileId);
            var changed = 0;
            foreach (var child in entityManager.GetComponent<TransformComponent>(grid.GridEntityId).ChildEntities)
            {
                if (!entityManager.EntityExists(child))
                {
                    continue;
                }

                if (!tagSystem.HasTag(child, WallTag))
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

                if (tileDef.ID == TilePrototypeId)
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
