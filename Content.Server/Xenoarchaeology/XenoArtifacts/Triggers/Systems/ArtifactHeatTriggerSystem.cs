using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactHeatTriggerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactHeatTriggerComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ArtifactHeatTriggerComponent, InteractUsingEvent>(OnUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQuery<ArtifactHeatTriggerComponent, TransformComponent, ArtifactComponent>();
        foreach (var (trigger, transform, artifact) in query)
        {
            var environment = _atmosphereSystem.GetTileMixture(transform.Coordinates);
            if (environment == null)
                continue;

            if (environment.Temperature < trigger.ActivationTemperature)
                continue;

            _artifactSystem.TryActivateArtifact(trigger.Owner, component: artifact);
        }
    }

    private void OnAttacked(EntityUid uid, ArtifactHeatTriggerComponent component, AttackedEvent args)
    {
        if (!component.ActivateHotItems || !CheckHot(args.Used))
            return;
        _artifactSystem.TryActivateArtifact(uid, args.User);
    }

    private void OnUsing(EntityUid uid, ArtifactHeatTriggerComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!component.ActivateHotItems || !CheckHot(args.Used))
            return;
        args.Handled = _artifactSystem.TryActivateArtifact(uid, args.User);
    }

    private bool CheckHot(EntityUid usedUid)
    {
        var hotEvent = new IsHotEvent();
        RaiseLocalEvent(usedUid, hotEvent, false);
        return hotEvent.IsHot;
    }
}
