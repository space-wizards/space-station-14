using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Construction.Commands;

[AdminCommand(AdminFlags.Mapping)]
sealed class TileReplaceCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "tilereplace";
    public string Description => "Replaces one tile with another.";
    public string Help => $"Usage: {Command} [<gridId>] <src> <dst>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player as IPlayerSession;
        EntityUid? gridId;
        string tileIdA;
        string tileIdB;

        switch (args.Length)
        {
            case 2:
                if (player?.AttachedEntity is not {Valid: true} playerEntity)
                {
                    shell.WriteLine("Only a player can run this command without a grid ID.");
                    return;
                }

                gridId = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                tileIdA = args[0];
                tileIdB = args[1];
                break;
            case 3:
                if (!NetEntity.TryParse(args[0], out var idNet) ||
                    !_entManager.TryGetEntity(idNet, out var id))
                {
                    shell.WriteLine($"{args[0]} is not a valid entity.");
                    return;
                }

                gridId = id;
                tileIdA = args[1];
                tileIdB = args[2];
                break;
            default:
                shell.WriteLine(Help);
                return;
        }

        var tileA = _tileDef[tileIdA];
        var tileB = _tileDef[tileIdB];

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

        var changed = 0;
        foreach (var tile in grid.GetAllTiles())
        {
            var tileContent = tile.Tile;
            if (tileContent.TypeId == tileA.TileId)
            {
                grid.SetTile(tile.GridIndices, new Tile(tileB.TileId));
                changed++;
            }
        }

        shell.WriteLine($"Changed {changed} tiles.");
    }
}

