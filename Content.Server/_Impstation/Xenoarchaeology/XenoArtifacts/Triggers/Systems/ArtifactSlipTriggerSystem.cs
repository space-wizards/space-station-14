using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Mobs;
using Content.Shared.Slippery;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

/// Slip trigger
public sealed class ArtifactSlipTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SlipperyComponent, SlipEvent>(OnSlip);
    }

    private void OnSlip(EntityUid uidSlippery, SlipperyComponent component, ref SlipEvent args)
    {

        EntityUid slipped = args.Slipped;

        var slippedXform = Transform(slipped);

        var toActivate = new List<Entity<ArtifactSlipTriggerComponent>>();
        var query = EntityQueryEnumerator<ArtifactSlipTriggerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var xform))
        {
            if (!slippedXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                continue;

            if (distance > trigger.Range)
                 continue;

            toActivate.Add((uid, trigger));
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a);
        }
    }
}
