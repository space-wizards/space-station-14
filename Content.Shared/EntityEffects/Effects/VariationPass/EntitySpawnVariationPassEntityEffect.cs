using System.Linq;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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

    protected override void Effect(Entity<MapGridComponent> entity, ref EntityEffectEvent<EntitySpawnVariationPass> args)
    {
        var totalTiles = _map.GetAllTiles(entity, entity).Count();

        var dirtyMod = _random.NextGaussian(args.Effect.TilesPerEntityAverage, args.Effect.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        for (var i = 0; i < trashTiles; i++)
        {
            FindRandomTileOnStation(entity, out var coords);

            var ents = EntitySpawnCollection.GetSpawns(args.Effect.Entities, _random);
            foreach (var spawn in ents)
            {
                SpawnAtPosition(spawn, coords);
            }
        }
    }

    private void FindRandomTileOnStation(Entity<MapGridComponent> grid,
        out EntityCoordinates targetCoords)
    {
        targetCoords = EntityCoordinates.Invalid;
        var aabb = grid.Comp.LocalAABB;

        // TODO: Check for atmospherics, which isn't available in Shared. Until that is done, this method isn't the same as the old one

        var randomX = _random.Next((int) aabb.Left, (int) aabb.Right);
        var randomY = _random.Next((int) aabb.Bottom, (int) aabb.Top);

        var tile = new Vector2i(randomX, randomY);

        targetCoords = _map.GridTileToLocal(grid, grid.Comp, tile);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EntitySpawnVariationPass : EntityEffectBase<EntitySpawnVariationPass>
{
    /// <summary>
    ///     Number of tiles before we spawn one entity on average.
    /// </summary>
    [DataField]
    public float TilesPerEntityAverage = 120f;

    [DataField]
    public float TilesPerEntityStdDev = 5f;

    /// <summary>
    ///     Spawn entries for each chosen location.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Entities = default!;
}
