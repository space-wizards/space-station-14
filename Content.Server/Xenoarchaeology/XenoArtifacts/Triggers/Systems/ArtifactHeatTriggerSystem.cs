using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactHeatTriggerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactHeatTriggerComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ArtifactHeatTriggerComponent, InteractUsingEvent>(OnUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<ArtifactComponent> toUpdate = new();
        foreach (var (trigger, transform, artifact) in EntityQuery<ArtifactHeatTriggerComponent, TransformComponent, ArtifactComponent>())
        {
            var uid = trigger.Owner;
            var environment = _atmosphereSystem.GetTileMixture(transform.GridUid, transform.MapUid,
                _transformSystem.GetGridOrMapTilePosition(uid, transform));
            if (environment == null)
                continue;

            if (environment.Temperature < trigger.ActivationTemperature)
                continue;

            toUpdate.Add(artifact);
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a.Owner, null, a);
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
        RaiseLocalEvent(usedUid, hotEvent);
        return hotEvent.IsHot;
    }
}
