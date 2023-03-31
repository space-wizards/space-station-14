using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactTimerTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactTimerTriggerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ArtifactTimerTriggerComponent component, ComponentStartup args)
    {
        component.LastActivation = _time.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<ArtifactComponent> toUpdate = new();
        foreach (var (trigger, artifact) in EntityQuery<ArtifactTimerTriggerComponent, ArtifactComponent>())
        {
            var timeDif = _time.CurTime - trigger.LastActivation;
            if (timeDif <= trigger.ActivationRate)
                continue;

            toUpdate.Add(artifact);
            trigger.LastActivation = _time.CurTime;
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a.Owner, null, a);
        }
    }
}
