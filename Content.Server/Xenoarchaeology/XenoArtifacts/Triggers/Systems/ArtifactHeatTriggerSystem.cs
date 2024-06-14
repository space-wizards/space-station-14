using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;
using Content.Shared.Temperature;
using Content.Shared.Weapons.Melee.Events;

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

        List<Entity<ArtifactComponent>> toUpdate = new();
        var query = EntityQueryEnumerator<ArtifactHeatTriggerComponent, TransformComponent, ArtifactComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var transform, out var artifact))
        {
            var environment = _atmosphereSystem.GetTileMixture((uid, transform));
            if (environment == null)
                continue;

            if (environment.Temperature < trigger.ActivationTemperature)
                continue;

            toUpdate.Add((uid, artifact));
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a, null, a);
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
