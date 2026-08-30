using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.VariationPass;

/// <summary>
/// Used for spawning entities randomly dotted around the grid.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class EntitySpawnVariationPassEntityEffectSystem : EntityEffectSystem<MapGridComponent, EntitySpawnVariationPass>
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityTableSystem _tables = default!;

    protected override void Effect(Entity<MapGridComponent> entity, ref EntityEffectEvent<EntitySpawnVariationPass> args)
    {
        var tiles = _map.GetAllTiles(entity, entity).ToList();
        var totalTiles = tiles.Count();

        var dirtyMod = _random.NextGaussian(args.Effect.TilesPerEntityAverage, args.Effect.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        for (var i = 0; i < trashTiles; i++)
        {
            if (TryFindRandomTile(entity, tiles, out var coords))
            {
                var ents = _tables.GetSpawns(args.Effect.Table, _random);
                foreach (var spawn in ents)
                {
                    SpawnAtPosition(spawn, coords);
                }
            }
        }
    }

    /// Attempts to find an empty tile 10 times, returns true if successful.
    private bool TryFindRandomTile(Entity<MapGridComponent> grid,
        List<TileRef> tiles,
        out EntityCoordinates targetCoords)
    {
        targetCoords = EntityCoordinates.Invalid;

        for (var i = 0; i < 10; i++)
        {
            var tile = _random.Pick(tiles);
            var tileCoords = tile.GridIndices;

            if (CheckTileEntities(grid, tileCoords))
            {
                targetCoords = _map.GridTileToLocal(grid, grid.Comp, tileCoords);
                return true;
            }
        }

        return false;
    }

    /// Returns false if the tile is occupied.
    private bool CheckTileEntities(Entity<MapGridComponent> grid, Vector2i tileCoords)
    {
        var intersectingEntities = new HashSet<EntityUid>();
        _lookup.GetLocalEntitiesIntersecting(grid, tileCoords, intersectingEntities, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in intersectingEntities)
        {
            if (TryComp<FixturesComponent>(ent, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    // Continue if no collision is possible
                    if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                        continue;

                    return false;
                }
            }
        }

        return true;
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EntitySpawnVariationPass : EntityEffectBase<EntitySpawnVariationPass>
{
    /// <summary>
    /// Number of tiles before we spawn one entity on average.
    /// </summary>
    [DataField]
    public float TilesPerEntityAverage = 50f;

    /// <summary>
    /// Standard deviation for the randomness selection.
    /// </summary>
    [DataField]
    public float TilesPerEntityStdDev = 7f;

    /// <summary>
    /// Spawn table for each chosen location.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}
