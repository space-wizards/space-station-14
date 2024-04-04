using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Storage;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="EntitySpawnVariationPassComponent"/>
public sealed class EntitySpawnVariationPassSystem : VariationPassSystem<EntitySpawnVariationPassComponent>
{
    protected override void ApplyVariation(Entity<EntitySpawnVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var totalTiles = Stations.GetTileCount(args.Station);

        var dirtyMod = Random.NextGaussian(ent.Comp.TilesPerEntityAverage, ent.Comp.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);
        if(!TryFindRandomTilesOnStation(args.Station, trashTiles, out var randomTiles))
        {
            return;
        }

        for (var i = 0; i < trashTiles; i++)
        {
            var curRandomTile = randomTiles.ElementAt(i);
            if (!TryComp<MapGridComponent>(curRandomTile.GridUid, out var curGridComp))
            {
                continue;
            }
            var coords = Map.GridTileToLocal(curRandomTile.GridUid, curGridComp, curRandomTile.GridIndices);

            var ents = EntitySpawnCollection.GetSpawns(ent.Comp.Entities, Random);
            foreach (var spawn in ents)
            {
                SpawnAtPosition(spawn, coords);
            }
        }
    }
}
