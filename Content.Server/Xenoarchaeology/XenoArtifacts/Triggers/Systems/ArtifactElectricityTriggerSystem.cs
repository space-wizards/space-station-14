using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactElectricityTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactElectricityTriggerComponent, PowerPulseEvent>(OnPowerPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityManager.EntityQuery<ArtifactElectricityTriggerComponent, PowerConsumerComponent, ArtifactComponent>();
        foreach (var (trigger, power, artifact) in query)
        {
            if (power.ReceivedPower <= 0)
                continue;

            _artifactSystem.TryActivateArtifact(trigger.Owner, component: artifact);
        }
    }

    private void OnPowerPulse(EntityUid uid, ArtifactElectricityTriggerComponent component, PowerPulseEvent args)
    {
        _artifactSystem.TryActivateArtifact(uid, args.User);
    }
}
