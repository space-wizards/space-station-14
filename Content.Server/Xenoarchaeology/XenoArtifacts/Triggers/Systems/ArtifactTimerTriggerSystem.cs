using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactTimerTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQuery<ArtifactTimerTriggerComponent, ArtifactComponent>();
        foreach (var (trigger, artifact) in query)
        {
            var timeDif = _time.CurTime - trigger.LastActivation;
            if (timeDif <= trigger.ActivationRate)
                continue;

            _artifactSystem.TryActivateArtifact(trigger.Owner, component: artifact);
            trigger.LastActivation = _time.CurTime;
        }
    }
}
