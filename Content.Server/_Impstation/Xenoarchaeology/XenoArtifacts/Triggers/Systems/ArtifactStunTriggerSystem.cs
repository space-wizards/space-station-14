using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Stunnable;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactStunTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StunnedComponent, StunnedEvent>(OnStun);
    }

    private void OnStun(EntityUid stunned, StunnedComponent component, ref StunnedEvent args)
    {

        var stunnedXform = Transform(stunned);

        var toActivate = new List<Entity<ArtifactStunTriggerComponent>>();
        var query = EntityQueryEnumerator<ArtifactStunTriggerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var xform))
        {
            if (!stunnedXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
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
