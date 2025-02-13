using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.ProximityDetection.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Shared.ProximityDetection.Systems;

/// <summary>
/// Handles generic proximity detector logic.
/// </summary>
public sealed class ProximityDetectionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProximityDetectorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ProximityDetectorComponent, ItemToggledEvent>(OnToggled);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnInit(Entity<ProximityDetectorComponent> ent, ref ComponentInit args)
    {
        var (_, component) = ent;

        component.NextUpdate = _timing.CurTime + component.UpdateCooldown;
        Dirty(ent);
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
            Dirty(uid, component);

            if (!_toggle.IsActivated(uid))
                continue;

            UpdateTarget((uid, component));
        }
    }

    private void ClearTarget(Entity<ProximityDetectorComponent> ent)
    {
        var (uid, comp) = ent;

        // Don't do anything if we have no target.
        if (comp.Target == null)
            return;

        comp.Distance = -1;
        comp.Target = null;

        var updatedEv = new ProximityTargetUpdatedEvent(-1, ent);
        RaiseLocalEvent(uid, ref updatedEv);

        var newTargetEv = new NewProximityTargetEvent(-1, ent);
        RaiseLocalEvent(uid, ref newTargetEv);

        Dirty(uid, comp);
    }

    private void UpdateTarget(Entity<ProximityDetectorComponent> detector)
    {
        var (uid, component) = detector;

        if (!_xformQuery.TryGetComponent(uid, out var transform))
            return;

        if (Deleted(component.Target))
            ClearTarget(detector);

        var worldPos = _transform.GetWorldPosition(transform);
        var closestDistance = -1f;
        EntityUid? closestEnt = null;

        foreach (var ent in _entityLookup.GetEntitiesInRange(uid, component.Range))
        {
            if (_whitelistSystem.IsWhitelistFail(component.Criteria, ent))
                continue;

            if (!_xformQuery.TryGetComponent(ent, out var xForm))
                continue;

            var dist = (_transform.GetWorldPosition(xForm) - worldPos).Length();

            if (dist < closestDistance)
                continue;

            var detectAttempt = new ProximityDetectionAttemptEvent(dist, detector, ent);
            RaiseLocalEvent(ent, ref detectAttempt);

            if (detectAttempt.Cancelled)
                continue;

            closestDistance = dist;
            closestEnt = ent;
        }

        var newDistance = component.Distance != closestDistance;
        var newTarget = component.Target != closestEnt;

        if (newDistance)
        {
            var updatedEv = new ProximityTargetUpdatedEvent(closestDistance, detector, closestEnt);
            RaiseLocalEvent(uid, ref updatedEv);

            component.Distance = closestDistance;
        }

        if (newTarget)
        {
            var newTargetEv = new NewProximityTargetEvent(closestDistance, detector, closestEnt);
            RaiseLocalEvent(uid, ref newTargetEv);

            component.Target = closestEnt;
        }

        if (newDistance || newTarget)
            Dirty(detector);
    }
}
