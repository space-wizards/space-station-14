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
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "tilewalls";
        public string Description => "Puts an underplating tile below every wall on a grid.";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        [ValidatePrototypeId<ContentTileDefinition>]
        public const string TilePrototypeId = "Plating";

        [ValidatePrototypeId<TagPrototype>]
        public const string WallTag = "Wall";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            EntityUid? gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteLine("Only a player can run this command.");
                        return;
                    }

                    gridId = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                    break;
                case 1:
                    if (!NetEntity.TryParse(args[0], out var idNet) || !_entManager.TryGetEntity(idNet, out var id))
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

            if (!_mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteLine($"No grid exists with id {gridId}");
                return;
            }

            if (!_entManager.EntityExists(grid.Owner))
            {
                shell.WriteLine($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var tagSystem = _entManager.EntitySysManager.GetEntitySystem<TagSystem>();
            var underplating = _tileDefManager[TilePrototypeId];
            var underplatingTile = new Tile(underplating.TileId);
            var changed = 0;
            foreach (var child in _entManager.GetComponent<TransformComponent>(grid.Owner).ChildEntities)
            {
                if (!_entManager.EntityExists(child))
                {
                    continue;
                }

                if (!tagSystem.HasTag(child, WallTag))
                {
                    continue;
                }

                var childTransform = _entManager.GetComponent<TransformComponent>(child);

                if (!childTransform.Anchored)
                {
                    continue;
                }

                var tile = grid.GetTileRef(childTransform.Coordinates);
                var tileDef = (ContentTileDefinition) _tileDefManager[tile.Tile.TypeId];

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
