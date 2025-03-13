using System.Linq;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="EntitySpawnVariationPassComponent"/>
public sealed class EntitySpawnVariationPassSystem : VariationPassSystem<EntitySpawnVariationPassComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!; // imp edit

    protected override void ApplyVariation(Entity<EntitySpawnVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var totalTiles = Stations.GetTileCount(args.Station);

        var dirtyMod = Random.NextGaussian(ent.Comp.TilesPerEntityAverage, ent.Comp.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        for (var i = 0; i < trashTiles; i++)
        {
            if (!TryFindRandomTileOnStation(args.Station, out _, out _, out var coords))
                continue;

            // imp edit
            var valid = true;

            if (ent.Comp.ComponentBlacklist != null)
            {
                foreach (var otherEnt in _lookup.GetEntitiesIntersecting(coords))
                {
                    foreach (var comp in ent.Comp.ComponentBlacklist.Values.Where(comp => HasComp(otherEnt, comp.Component.GetType())))
                    {
                        if (!valid)
                            continue;

                        valid = false;
                    }
                }
            }

            if (!valid)
                continue;
            // end imp edit

            var ents = EntitySpawnCollection.GetSpawns(ent.Comp.Entities, Random);
            foreach (var spawn in ents)
            {
                SpawnAtPosition(spawn, coords);
            }
        }
    }
}
