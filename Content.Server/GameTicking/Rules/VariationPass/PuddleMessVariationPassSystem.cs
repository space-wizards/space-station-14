using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Random.Helpers;
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
        var totalTiles = Stations.GetTileCount(args.Station);

        if (!_proto.TryIndex(ent.Comp.RandomPuddleSolutionFill, out var proto))
            return;

        var puddleMod = Random.NextGaussian(ent.Comp.TilesPerSpillAverage, ent.Comp.TilesPerSpillStdDev);
        var puddleTiles = Math.Max((int) (totalTiles * (1 / puddleMod)), 0);

        for (var i = 0; i < puddleTiles; i++)
        {
            if (!TryFindRandomTileOnStation(args.Station, out _, out _, out var coords))
                continue;

            var sol = proto.Pick(Random);
            _puddle.TrySpillAt(coords, new Solution(sol.reagent, sol.quantity), out _, sound: false);
        }
    }
}
