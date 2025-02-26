using Content.Shared.Mobs.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEShuffleSystem : BaseXAESystem<XAEShuffleComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEShuffleComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if(!_timing.IsFirstTimePredicted)
            return;

        var mobState = GetEntityQuery<MobStateComponent>();

        List<Entity<TransformComponent>> toShuffle = new();

        foreach (var entity in _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (!mobState.HasComponent(entity))
                continue;

            var xform = Transform(entity);

            toShuffle.Add((entity, xform));
        }

        _random.Shuffle(toShuffle);

        while (toShuffle.Count > 1)
        {
            var ent1 = _random.PickAndTake(toShuffle);
            var ent2 = _random.PickAndTake(toShuffle);
            _xform.SwapPositions((ent1, ent1), (ent2, ent2));
        }
    }
}
