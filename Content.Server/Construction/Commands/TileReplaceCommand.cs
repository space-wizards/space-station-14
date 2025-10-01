using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Construction.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class TileReplaceCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "tilereplace";
    public string Description => "Replaces one tile with another.";
    public string Help => $"Usage: {Command} [<gridId>] <src> <dst>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        EntityUid? gridId;
        string tileIdA;
        string tileIdB;

        switch (args.Length)
        {
            case 2:
                if (player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError("Only a player can run this command without a grid ID.");
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
                    shell.WriteError($"{args[0]} is not a valid entity.");
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

        if (!_entManager.TryGetComponent(gridId, out MapGridComponent? grid))
        {
            shell.WriteError($"No grid exists with id {gridId}");
            return;
        }

        if (!_entManager.EntityExists(gridId))
        {
            shell.WriteError($"Grid {gridId} doesn't have an associated grid entity.");
            return;
        }

        var mapSystem = _entManager.System<SharedMapSystem>();

        var changed = 0;
        foreach (var tile in mapSystem.GetAllTiles(gridId.Value, grid))
        {
            var tileContent = tile.Tile;
            if (tileContent.TypeId == tileA.TileId)
            {
                mapSystem.SetTile(gridId.Value, grid, tile.GridIndices, new Tile(tileB.TileId));
                changed++;
            }
        }

        shell.WriteLine($"Changed {changed} tiles.");
    }
}

