﻿using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.ProximityDetection.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.ProximityDetection.Systems;

/// <summary>
/// Handles generic proximity detector logic.
/// </summary>
public sealed class ProximityDetectionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProximityDetectorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ProximityDetectorComponent, NewProximityTargetEvent>(OnNewTarget);
        SubscribeLocalEvent<ProximityDetectorComponent, ItemToggledEvent>(OnToggled);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnMapInit(Entity<ProximityDetectorComponent> ent, ref MapInitEvent args)
    {
        var component = ent.Comp;

        component.NextUpdate = _timing.CurTime + component.UpdateCooldown;
        DirtyField(ent, component, nameof(ProximityDetectorComponent.NextUpdate));
    }

    private void OnNewTarget(Entity<ProximityDetectorComponent> ent, ref NewProximityTargetEvent args)
    {
        if (ent.Comp.Target.HasValue)
            _pvsOverride.RemoveGlobalOverride(ent.Comp.Target.Value);

        if (args.Target.HasValue)
            _pvsOverride.AddGlobalOverride(args.Target.Value);
    }

    private void OnToggled(Entity<ProximityDetectorComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            UpdateTarget(ent);
        else
            ClearTarget(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ProximityDetectorComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            component.NextUpdate += component.UpdateCooldown;
            DirtyField(uid, component, nameof(ProximityDetectorComponent.NextUpdate));

            if (!_toggle.IsActivated(uid))
                continue;

            UpdateTarget((uid, component));
        }
    }

    private void ClearTarget(Entity<ProximityDetectorComponent> ent)
    {
        var component = ent.Comp;

        // Don't do anything if we have no target.
        if (component.Target == null)
            return;

        var newTargetEv = new NewProximityTargetEvent(0, ent);
        RaiseLocalEvent(ent, ref newTargetEv);

        var updatedEv = new ProximityTargetUpdatedEvent(0, ent);
        RaiseLocalEvent(ent, ref updatedEv);

        component.Target = null;
        component.Distance = 0;
        DirtyFields(ent, component, null, nameof(ProximityDetectorComponent.Target), nameof(ProximityDetectorComponent.Distance));
    }

    private void UpdateTarget(Entity<ProximityDetectorComponent> detector)
    {
        var component = detector.Comp;

        if (!_xformQuery.TryGetComponent(detector, out var transform))
            return;

        if (Deleted(component.Target))
            ClearTarget(detector);

        var closestDistance = float.PositiveInfinity;
        EntityUid? closestUid = null;

        var query = EntityManager.CompRegistryQueryEnumerator(component.Components);

        while (query.MoveNext(out var uid))
        {
            if (!_xformQuery.TryGetComponent(uid, out var xForm))
                continue;

            if (!transform.Coordinates.TryDistance(EntityManager, xForm.Coordinates, out var distance) ||
                distance > component.Range || distance >= closestDistance)
                continue;

            var detectAttempt = new ProximityDetectionAttemptEvent(distance, detector, uid);
            RaiseLocalEvent(detector, ref detectAttempt);

            if (detectAttempt.Cancelled)
                continue;

            closestDistance = distance;
            closestUid = uid;
        }

        if (component.Target != closestUid)
        {
            var newTargetEv = new NewProximityTargetEvent(closestDistance, detector, closestUid);
            RaiseLocalEvent(detector, ref newTargetEv);

            component.Target = closestUid;
            DirtyField(detector, component, nameof(ProximityDetectorComponent.Target));
        }

        if (component.Distance != closestDistance)
        {
            var updatedEv = new ProximityTargetUpdatedEvent(closestDistance, detector, closestUid);
            RaiseLocalEvent(detector, ref updatedEv);

            component.Distance = closestDistance;
            DirtyField(detector, component, nameof(ProximityDetectorComponent.Distance));
        }
    }
}
