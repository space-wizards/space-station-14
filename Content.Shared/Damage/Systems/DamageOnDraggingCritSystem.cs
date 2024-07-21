using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Gravity;

namespace Content.Shared.Damage.Systems;

// This system applies bruise damage and worsens bleeding on mobs that are pulled
// on the ground when they are crit/dead
public sealed class DamageOnDraggingCritSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    private EntityQuery<PullableComponent> _pullableQuery;
    private EntityQuery<MobThresholdsComponent> _thresholdsQuery;

    public override void Initialize()
    {
        _pullableQuery = GetEntityQuery<PullableComponent>();
        _thresholdsQuery = GetEntityQuery<MobThresholdsComponent>();

        SubscribeLocalEvent<DamageOnDraggingCritComponent, MoveEvent>(OnPulledMove);
        SubscribeLocalEvent<DamageOnDraggingCritComponent, PullMessage>(OnPullMessage);
    }

    public override void Update(float frameTime) {
        var query = EntityQueryEnumerator<DamageOnDraggingCritComponent>();
        while(query.MoveNext(out var uid, out var component)) {
            if(!component.Enabled)
                continue;

            component.IntervalTimer += frameTime;
            if(component.IntervalTimer >= component.Interval) {
                CheckForDamage(uid, component);
                ResetComponent(component);
            }
        }
    }

    // Clear state data when pull begins/ends
    private void OnPullMessage(EntityUid _uid, DamageOnDraggingCritComponent comp, PullMessage _msg) => ResetComponent(comp);

    private void ResetComponent(DamageOnDraggingCritComponent comp) {
        comp.IntervalTimer = 0.0f;
        comp.DistanceDragged = 0.0f;
    }

    private void CheckForDamage(EntityUid uid, DamageOnDraggingCritComponent comp) {
        if (!_pullableQuery.TryComp(uid, out var pullable))
            return;
        if (!_thresholdsQuery.TryComp(uid, out var thresholds))
            return;

        // Check that we are being pulled
        if(!pullable.BeingPulled)
            return;

        // Check that we are crit or dead
        var mobState = thresholds.CurrentThresholdState;
        if (mobState != MobState.Critical && mobState != MobState.Dead)
            return;

        // Check that we are not weightless
        if(_gravity.IsWeightless(uid))
            return;

        var adjustedThreshold = comp.DistanceThreshold * comp.Interval;
        //Log.Info($"Distance dragged: {comp.DistanceDragged} / {adjustedThreshold}");

        if(comp.DistanceDragged >= adjustedThreshold)
            _damageable.TryChangeDamage(uid, comp.Damage * comp.IntervalTimer, origin: pullable.Puller);
    }

    private void OnPulledMove(EntityUid uid, DamageOnDraggingCritComponent comp, ref MoveEvent args)
    {
        if (!args.OldPosition.TryDistance(_entMan, args.NewPosition, out var drag_dist))
            return;

        comp.DistanceDragged += drag_dist;
    }
}
