using System.Threading;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Damage;
using Content.Server.Damage.Components;
using Content.Shared.Gravity;
using Content.Shared.Effects;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Damage.Systems;

// This system applies blunt damage on mobs that are pulled
// on the ground when they are crit/dead
public sealed class DamageOnDraggingCritSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    //[Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<PullableComponent> _pullableQuery;
    private EntityQuery<MobThresholdsComponent> _thresholdsQuery;

    public override void Initialize()
    {
        _pullableQuery = GetEntityQuery<PullableComponent>();
        _thresholdsQuery = GetEntityQuery<MobThresholdsComponent>();

        SubscribeLocalEvent<DamageOnDraggingCritComponent, MoveEvent>(OnPulledMove);
        SubscribeLocalEvent<DamageOnDraggingCritComponent, PullStartedMessage>(OnPullStart);
        SubscribeLocalEvent<DamageOnDraggingCritComponent, PullStoppedMessage>(OnPullStop);
    }

    private void ResetComponent(DamageOnDraggingCritComponent comp) {
        comp.TimerCancel?.Cancel();
        comp.DistanceDragged = 0.0f;
    }

    private void StartIntervalTimer(EntityUid uid, DamageOnDraggingCritComponent comp) {
        ResetComponent(comp);
        comp.TimerCancel = new CancellationTokenSource();
        comp.LastInterval = _gameTiming.CurTime;

        Timer.Spawn(comp.Interval, () => {
            CheckForDamage(uid, comp);
            StartIntervalTimer(uid, comp);
        }, comp.TimerCancel.Token);
    }

    private void OnPullStart(EntityUid uid, DamageOnDraggingCritComponent comp, PullStartedMessage args) {
        if(args.PulledUid != uid)
            return;

        //Log.Info("Beginning damage checks for pulling");
        ResetComponent(comp);
        StartIntervalTimer(uid, comp);
    }

    private void OnPullStop(EntityUid uid, DamageOnDraggingCritComponent comp, PullStoppedMessage args) {
        if(args.PulledUid != uid)
            return;

        //Log.Info("Ending damage checks for pulling");
        ResetComponent(comp);
    }

    private void OnPulledMove(EntityUid uid, DamageOnDraggingCritComponent comp, ref MoveEvent args)
    {
        if (!args.OldPosition.TryDistance(_entMan, args.NewPosition, out var drag_dist))
            return;

        comp.DistanceDragged += drag_dist;
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
        
        // Check that we're not wearing a hardsuit
        // We don't want to make salvage's job harder
        if(_inventory.TryGetSlotEntity(uid, "outerClothing", out var suit) && _tag.HasTag(suit.Value, "Hardsuit"))
            return;

        var adjustedThreshold = comp.DistanceThreshold * comp.Interval.TotalSeconds;
        //Log.Info($"Distance dragged: {comp.DistanceDragged} / {adjustedThreshold}");

        if(comp.DistanceDragged >= adjustedThreshold)
        {
            _damageable.TryChangeDamage(uid, comp.Damage * (_gameTiming.CurTime - comp.LastInterval).TotalSeconds, origin: pullable.Puller);
            _color.RaiseEffect(Color.Red, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
            //_audio.PlayPvs(comp.DragSound, uid);
        }
    }
}
