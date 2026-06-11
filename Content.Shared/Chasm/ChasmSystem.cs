using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chasm;

/// <summary>
///     Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed partial class ChasmSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGrapplingGunSystem _grapple = default!;

    [Dependency] private EntityQuery<ChasmComponent> _chasmQuery;
    [Dependency] private EntityQuery<ChasmFallingComponent> _chasmFallingQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<ChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ChasmFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextDeletionTime)
                continue;

            var chasmEvent = new EntityCompletedFallingIntoChasmEvent((uid, chasm));
            RaiseLocalEvent(chasm.FallingInto, ref chasmEvent);
            if (_chasmQuery.TryComp(chasm.FallingInto, out var chasmComp))
            {
                var tripperEvent = new CompletedFallingIntoChasmEvent((chasm.FallingInto, chasmComp));
                RaiseLocalEvent(uid, ref tripperEvent);
            }
            else
            {
                DebugTools.Assert($"{ToPrettyString(chasm.FallingInto)} is missing {nameof(ChasmComponent)}");
            }

            PredictedQueueDel(uid);
        }
    }

    private void OnStepTriggered(Entity<ChasmComponent> entity, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (_chasmFallingQuery.HasComp(args.Tripper))
            return;

        StartFalling(entity, args.Tripper);
    }

    /// <summary>
    /// Causes <paramref name="tripper"/> to fall into <paramref name="chasm"/>: starts a falling animation, optionally
    /// plays a sound, and eventually deletes <paramref name="tripper"/>.
    /// If <paramref name="chasm"/> does not have a <see cref="ChasmComponent"/> component, does nothing.
    /// </summary>
    [PublicAPI]
    public void StartFalling(Entity<ChasmComponent?> chasm, EntityUid tripper, bool playSound = true)
    {
        if (!_chasmQuery.Resolve(chasm, ref chasm.Comp, logMissing: false))
            return;

        var falling = AddComp<ChasmFallingComponent>(tripper);
        falling.FallingInto = chasm;

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(tripper);

        if (playSound)
            _audio.PlayPredicted(chasm.Comp.FallingSound, chasm, tripper);

        var chasmEvent = new EntityStartedFallingIntoChasmEvent((tripper, falling));
        RaiseLocalEvent(chasm, ref chasmEvent);
        var tripperEvent = new StartedFallingIntoChasmEvent(chasm);
        RaiseLocalEvent(tripper, ref tripperEvent);

        Dirty(tripper, falling);
    }

    private void OnStepTriggerAttempt(Entity<ChasmComponent> entity, ref StepTriggerAttemptEvent args)
    {
        if (_grapple.IsEntityHooked(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }

    private static void OnUpdateCanMove(Entity<ChasmFallingComponent> entity, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }
}
