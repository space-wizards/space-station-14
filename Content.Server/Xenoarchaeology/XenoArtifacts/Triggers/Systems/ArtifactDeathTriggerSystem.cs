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

        var toActivate = new List<ArtifactDeathTriggerComponent>();
        foreach (var (trigger, xform) in EntityQuery<ArtifactDeathTriggerComponent, TransformComponent>())
        {
            if (!deathXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                continue;

            if (distance > trigger.Range)
                continue;

            toActivate.Add(trigger);
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a.Owner);
        }
    }
}
