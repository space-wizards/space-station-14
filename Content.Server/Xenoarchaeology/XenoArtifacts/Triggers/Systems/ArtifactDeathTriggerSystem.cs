using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Mobs;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactDeathTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        var deathXform = Transform(ev.Target);

        var toActivate = new List<Entity<ArtifactDeathTriggerComponent>>();
        var query = EntityQueryEnumerator<ArtifactDeathTriggerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var xform))
        {
            if (!deathXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
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
