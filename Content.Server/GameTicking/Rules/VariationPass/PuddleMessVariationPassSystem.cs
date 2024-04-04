using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="PuddleMessVariationPassComponent"/>
public sealed class PuddleMessVariationPassSystem : VariationPassSystem<PuddleMessVariationPassComponent>
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void ApplyVariation(Entity<PuddleMessVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var totalTiles = Stations.GetTileCount(args.Station);

        if (!_proto.TryIndex(ent.Comp.RandomPuddleSolutionFill, out var proto))
            return;

        var puddleMod = Random.NextGaussian(ent.Comp.TilesPerSpillAverage, ent.Comp.TilesPerSpillStdDev);
        var puddleTiles = Math.Max((int) (totalTiles * (1 / puddleMod)), 0);
        if(!TryFindRandomTilesOnStation(args.Station, puddleTiles, out var randomTiles))
        {
            return;
        }

        for (var i = 0; i < puddleTiles; i++)
        {
            var curRandomTile = randomTiles.ElementAt(i);
            if (!TryComp<MapGridComponent>(curRandomTile.GridUid, out var curGridComp))
            {
                continue;
            }
            var coords = Map.GridTileToLocal(curRandomTile.GridUid, curGridComp, curRandomTile.GridIndices);

            var sol = proto.Pick(Random);
            _puddle.TrySpillAt(coords, new Solution(sol.reagent, sol.quantity), out _, sound: false);
        }
    }
}
