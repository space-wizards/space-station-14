using System.Linq;
using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using static Content.Server.Maps.TileShapes;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Creates a grid filled with a width x height rectangle of tiles.
/// Example uses:
/// rectgrid:make_ent [4, 1] 3 PlatingDamaged
/// ent 3 | rectgrid:make_coords [4, 1] [30, 10]
/// </summary>
[ToolshedCommand] [AdminCommand(AdminFlags.Debug)]
public sealed partial class RectGridCommand : ToolshedCommand
{
    private static readonly ProtoId<ContentTileDefinition> DefaultTile = "Plating";
    [Dependency] private ITileDefinitionManager _tileDefs = null!;
    private SharedMapSystem Map => field ??= GetSys<SharedMapSystem>();
    private SharedTransformSystem Transforms => field ??= GetSys<SharedTransformSystem>();

    [CommandImplementation("make_coords")]
    public EntityUid MakeAtCoords([PipedArgument] EntityUid mapUid,
        Vector2i dimensions,
        Vector2 position,
        ProtoId<ContentTileDefinition> tile = default)
    {
        var where = new EntityCoordinates(mapUid, position);
        return CreateRect(dimensions, where, tile);
    }

    [CommandImplementation("make_ent")]
    public EntityUid MakeAtEnt([CommandInvocationContext] IInvocationContext ctx,
        Vector2i dimensions,
        EntityUid target,
        ProtoId<ContentTileDefinition> tile = default)
    {
        if (Transform(target).MapUid is { } mapUid)
            return MakeAtCoords(mapUid, dimensions, Transforms.GetWorldPosition(target), tile);
        ctx.WriteLine("Target is not on the map");
        return EntityUid.Invalid;
    }

    private EntityUid CreateRect(Vector2i dimensions, EntityCoordinates where, ProtoId<ContentTileDefinition> tile)
    {
        var name = string.IsNullOrEmpty(tile.Id) ? DefaultTile : tile;
        var tileId = _tileDefs[name].TileId;
        var grid = Map.CreateGridEntity(where.EntityId);
        var tiles = Rectangle(dimensions.X, dimensions.Y, tileId).ToList();
        Map.SetTiles(grid, tiles);
        Transforms.SetWorldPosition(grid.Owner, where.Position);
        return grid.Owner;
    }
}
