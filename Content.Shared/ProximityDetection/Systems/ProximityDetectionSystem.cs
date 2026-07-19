using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.ProximityDetection.Components;
using Robust.Shared.Timing;

namespace Content.Shared.ProximityDetection.Systems;

/// <summary>
/// Handles generic proximity detector logic.
/// </summary>
public sealed partial class ProximityDetectionSystem : EntitySystem
{
    private static readonly EntityTimerId UpdateTimer = new("update");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProximityDetectorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ProximityDetectorComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ProximityDetectorComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<ProximityDetectorComponent> ent, ref MapInitEvent args)
    {
        var component = ent.Comp;

        component.NextUpdate = _timing.CurTime + component.UpdateCooldown;
        DirtyField(ent, component, nameof(ProximityDetectorComponent.NextUpdate));
        _timers.SetTimerAt(ent, UpdateTimer, component.NextUpdate);
    }

    private void OnToggled(Entity<ProximityDetectorComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            UpdateTarget(ent);
        else
            ClearTarget(ent);
    }

    private void OnTimer(Entity<ProximityDetectorComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != UpdateTimer)
            return;

        ent.Comp.NextUpdate = args.ScheduledTime + ent.Comp.UpdateCooldown;
        DirtyField(ent, ent.Comp, nameof(ProximityDetectorComponent.NextUpdate));
        _timers.SetTimerAt(ent, UpdateTimer, ent.Comp.NextUpdate);

        if (_toggle.IsActivated(ent.Owner))
            UpdateTarget(ent);
    }

    private void ClearTarget(Entity<ProximityDetectorComponent> ent)
    {
        var component = ent.Comp;

        // Don't do anything if we have no target.
        if (component.Target == null)
            return;

        component.Distance = float.PositiveInfinity;
        DirtyField(ent, component, nameof(ProximityDetectorComponent.Distance));

        component.Target = null;
        DirtyField(ent, component, nameof(ProximityDetectorComponent.Target));

        var updatedEv = new ProximityTargetUpdatedEvent(component.Distance, ent);
        RaiseLocalEvent(ent, ref updatedEv);

        var newTargetEv = new NewProximityTargetEvent(component.Distance, ent);
        RaiseLocalEvent(ent, ref newTargetEv);
    }

    private void UpdateTarget(Entity<ProximityDetectorComponent> detector)
    {
        var component = detector.Comp;

        if (!TryComp(detector, out TransformComponent? transform))
            return;

        if (Deleted(component.Target))
            ClearTarget(detector);

        var closestDistance = float.PositiveInfinity;
        EntityUid? closestUid = null;

        var query = EntityManager.CompRegistryQueryEnumerator(component.Components);

        while (query.MoveNext(out var uid))
        {
            if (!TryComp(uid, out TransformComponent? xForm))
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

        var newDistance = component.Distance != closestDistance;
        var newTarget = component.Target != closestUid;

        if (newDistance)
        {
            var updatedEv = new ProximityTargetUpdatedEvent(closestDistance, detector, closestUid);
            RaiseLocalEvent(detector, ref updatedEv);

            component.Distance = closestDistance;
            DirtyField(detector, component, nameof(ProximityDetectorComponent.Distance));
        }

        if (newTarget)
        {
            var newTargetEv = new NewProximityTargetEvent(closestDistance, detector, closestUid);
            RaiseLocalEvent(detector, ref newTargetEv);

            component.Target = closestUid;
            DirtyField(detector, component, nameof(ProximityDetectorComponent.Target));
        }
    }
}
