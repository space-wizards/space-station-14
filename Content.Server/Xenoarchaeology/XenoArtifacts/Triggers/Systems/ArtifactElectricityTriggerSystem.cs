using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Server.Tools.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactElectricityTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactElectricityTriggerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ArtifactElectricityTriggerComponent, PowerPulseEvent>(OnPowerPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityManager.EntityQuery<ArtifactElectricityTriggerComponent, PowerConsumerComponent, ArtifactComponent>();
        foreach (var (trigger, power, artifact) in query)
        {
            if (power.ReceivedPower <= trigger.MinPower)
                continue;

            _artifactSystem.TryActivateArtifact(trigger.Owner, component: artifact);
        }
    }

    private void OnInteractUsing(EntityUid uid, ArtifactElectricityTriggerComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing"))
            return;

        args.Handled = _artifactSystem.TryActivateArtifact(uid, args.User);
    }

    private void OnPowerPulse(EntityUid uid, ArtifactElectricityTriggerComponent component, PowerPulseEvent args)
    {
        _artifactSystem.TryActivateArtifact(uid, args.User);
    }
}
