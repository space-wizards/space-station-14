using System.Linq;
using Content.Server.Worldgen.Floorplanners;
using Content.Shared.Maps;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Floorplanners;

/// <summary>
/// This handles...
/// </summary>
public sealed class AsteroidFloorplannerSystem : EntitySystem, IFloorplanSystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public bool ConstructTiling(FloorplanConfig rawConfig, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds, out object? planData)
    {
        var config = (AsteroidFloorplanConfig) rawConfig;
                var grid = _mapManager.GetGrid(targetGrid);

        var startPoint = centerPoint.Floored();
        var tileProto = _prototypeManager.Index<WeightedRandomPrototype>(config.TileWeightList);

        // NO MORE THAN TWO ALLOCATIONS THANK YOU VERY MUCH.
        var spawnPoints = new HashSet<Vector2i>(config.FloorPlacements * 4);
        var taken = new HashSet<Vector2i>(config.FloorPlacements);

        void PlaceTile(Vector2i point)
        {
            // Assume we already know that the spawn point is safe.
            spawnPoints.Remove(point);
            taken.Add(point);
            var north = point.Offset(Direction.North);
            var south = point.Offset(Direction.South);
            var east = point.Offset(Direction.East);
            var west = point.Offset(Direction.West);
            var radsq = MathF.Pow(config.Radius, 2); // I'd put this outside but i'm not 100% certain caching it between calls is a gain.

            // The math done is essentially a fancy way of comparing the distance from 0,0 to the radius,
            // and skipping the sqrt normally needed for dist.
            if (!taken.Contains(north) && (bounds?.ContainsPoint(north) ?? true) && MathF.Pow(north.X - startPoint.X, 2) + MathF.Pow(north.Y - startPoint.Y, 2) <= radsq)
                spawnPoints.Add(north);
            if (!taken.Contains(south) && (bounds?.ContainsPoint(south) ?? true) && MathF.Pow(south.X - startPoint.X, 2) + MathF.Pow(south.Y - startPoint.Y, 2) <= radsq)
                spawnPoints.Add(south);
            if (!taken.Contains(east) && (bounds?.ContainsPoint(east) ?? true) && MathF.Pow(east.X - startPoint.X, 2) + MathF.Pow(east.Y - startPoint.Y, 2) <= radsq)
                spawnPoints.Add(east);
            if (!taken.Contains(west) && (bounds?.ContainsPoint(west) ?? true) && MathF.Pow(west.X - startPoint.X, 2) + MathF.Pow(west.Y - startPoint.Y, 2) <= radsq)
                spawnPoints.Add(west);
        }

        PlaceTile(startPoint);

        for (var i = 0; i < config.FloorPlacements; i++)
        {
            var point = _random.Pick(spawnPoints);
            PlaceTile(point);

            if (config.SmoothResult)
            {
                if (!taken.Contains(point.Offset(Direction.North)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.North));
                if (!taken.Contains(point.Offset(Direction.South)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.South));
                if (!taken.Contains(point.Offset(Direction.East)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.East));
                if (!taken.Contains(point.Offset(Direction.West)) && _random.Prob(0.5f))
                    PlaceTile(point.Offset(Direction.West));
            }
        }

        grid.SetTiles(taken.Select(x => {
            var def = (ContentTileDefinition)_tileDefinition[tileProto.Pick(_random)];

            return (x, new Tile(def.TileId, 0, _random.Pick(def.PlacementVariants)));
        }).ToList());

        planData = null; // Asteroids don't do any complex planning atm.
        return true;
    }

    public void Populate(FloorplanConfig config, EntityUid targetGrid, Vector2 centerPoint, Constraint? bounds,
        out object? planData)
    {
        throw new NotImplementedException();
    }
}
