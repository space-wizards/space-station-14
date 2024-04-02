using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="PuddleMessVariationPassComponent"/>
public sealed class PuddleMessVariationPassSystem : VariationPassSystem<PuddleMessVariationPassComponent>
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void ApplyVariation(Entity<PuddleMessVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var largestGridTiles = GetAllTilesFromLargestGrid(ent, args.Station, out var largestGridComponent);
        if (largestGridTiles is null || largestGridComponent is null)
        {
            return;
        }
        if (!_proto.TryIndex(ent.Comp.RandomPuddleSolutionFill, out var proto))
            return;

        var totalTiles = largestGridTiles.Count();
        var puddleMod = Random.NextGaussian(ent.Comp.TilesPerSpillAverage, ent.Comp.TilesPerSpillStdDev);
        var puddleTiles = Math.Max((int) (totalTiles * (1 / puddleMod)), 0);

        var largestGridRandomTiles = GetRandomTiles(largestGridTiles, puddleTiles);

        for (var i = 0; i < puddleTiles; i++)
        {
            var curTileRef = largestGridRandomTiles.ElementAt(i);
            var coords = Map.GridTileToLocal(args.Station, largestGridComponent, curTileRef.GridIndices);

            var sol = proto.Pick(Random);
            _puddle.TrySpillAt(coords, new Solution(sol.reagent, sol.quantity), out _, sound: false);
        }
    }
}
