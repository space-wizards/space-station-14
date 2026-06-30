using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Storage;
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
                var ents = EntitySpawnCollection.GetSpawns(args.Effect.Entities, _random);
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

        var found = false;

        for (var i = 0; i < 10; i++)
        {
            var tile = _random.Pick(tiles);
            var tileCoords = tile.GridIndices;

            var intersectingEntities = new HashSet<EntityUid>();
            _lookup.GetLocalEntitiesIntersecting(grid, tileCoords, intersectingEntities, -0.05f, LookupFlags.Uncontained);

            var blocker = false;
            foreach (var ent in intersectingEntities)
            {
                if (TryComp<FixturesComponent>(ent, out var fixtures))
                {
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        // Continue if no collision is possible
                        if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                            continue;

                        blocker = true;
                        break;
                    }
                }

                if (blocker)
                    break;
            }

            if (!blocker)
            {
                found = true;
                targetCoords = _map.GridTileToLocal(grid, grid.Comp, tileCoords);
                return found;
            }
        }

        return found;
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EntitySpawnVariationPass : EntityEffectBase<EntitySpawnVariationPass>
{
    /// <summary>
    ///     Number of tiles before we spawn one entity on average.
    /// </summary>
    [DataField]
    public float TilesPerEntityAverage = 50f;

    [DataField]
    public float TilesPerEntityStdDev = 7f;

    /// <summary>
    ///     Spawn entries for each chosen location.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Entities = default!;
}
