using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
/// This handles...
/// </summary>
public sealed class TrashVariationPassSystem : VariationPassSystem<TrashVariationPassComponent>
{
    protected override void ApplyVariation(Entity<TrashVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var totalTiles = Stations.GetTileCount(args.Station);

        var dirtyMod = Random.NextGaussian(ent.Comp.TilesPerTrashAverage, ent.Comp.TilesPerTrashStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        for (var i = 0; i < trashTiles; i++)
        {
            if (!TryFindRandomTileOnStation(args.Station, out _, out _, out var coords))
                continue;

            SpawnAtPosition("RandomSpawner", coords);
        }
    }
}
