using System.Linq;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="EntitySpawnVariationPassComponent"/>
public sealed class EntitySpawnVariationPassSystem : VariationPassSystem<EntitySpawnVariationPassComponent>
{
    protected override void ApplyVariation(Entity<EntitySpawnVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var largestGridTiles = GetAllTilesFromLargestGrid(ent, args.Station, out var largestGridComponent);

        if (largestGridTiles is null || largestGridComponent is null)
        {
            return;
        }

        var totalTiles = largestGridTiles.Count();
        var dirtyMod = Random.NextGaussian(ent.Comp.TilesPerEntityAverage, ent.Comp.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        var largestGridRandomTiles = GetRandomTiles(largestGridTiles, trashTiles);

        for (var i = 0; i < trashTiles; i++)
        {
            var curTileRef = largestGridRandomTiles.ElementAt(i);
            var coords = Map.GridTileToLocal(args.Station, largestGridComponent, curTileRef.GridIndices);

            var ents = EntitySpawnCollection.GetSpawns(ent.Comp.Entities, Random);
            foreach (var spawn in ents)
            {
                SpawnAtPosition(spawn, coords);
            }
        }
    }
}
