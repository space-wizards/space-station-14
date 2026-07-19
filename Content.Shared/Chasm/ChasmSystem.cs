using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Chasm;

/// <summary>
/// Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed partial class ChasmSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedGrapplingGunSystem _grapple = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<ChasmFallingComponent> _chasmFallingQuery;
    [Dependency] private EntityQuery<ChasmComponent> _chasmQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<ChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ChasmComponent, ComponentShutdown>(OnShutdown);

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

        // Check the white-/blacklists and inform on rejection.
        if (!(entity.Comp.Whitelist == null && entity.Comp.Blacklist == null ||
              _whitelist.CheckBoth(args.Tripper, entity.Comp.Blacklist, entity.Comp.Whitelist)))
        {
            var rejected = new FallerRejectedByChasmEvent(args.Tripper);
            RaiseLocalEvent(entity, ref rejected);
            return;
        }

        // Give an opportunity to cancel the fall for whatever reason.
        var checkEvent = new EntityStartFallingAttemptEvent(args.Tripper);
        RaiseLocalEvent(entity, ref checkEvent);
        if (checkEvent.Cancelled)
            return;

        StartFalling(entity.AsNullable(), args.Tripper);
    }

    /// <summary>
    /// Causes <paramref name="tripper"/> to fall into <paramref name="chasm"/>: starts a falling animation, optionally
    /// plays a sound, and eventually deletes <paramref name="tripper"/>.
    /// If <paramref name="chasm"/> does not have a <see cref="ChasmComponent"/> component, does nothing and returns null.
    /// </summary>
    /// <returns>
    /// <paramref name="tripper"/> with its new <see cref="ChasmFallingComponent"/>, if the entity did start falling. Null otherwise.
    /// </returns>
    [PublicAPI]
    public Entity<ChasmFallingComponent>? StartFalling(
        Entity<ChasmComponent?> chasm,
        EntityUid tripper,
        bool playSound = true
    )
    {
        if (!_chasmQuery.Resolve(chasm, ref chasm.Comp, logMissing: false))
            return null;

        var falling = AddComp<ChasmFallingComponent>(tripper);
        falling.FallingInto = chasm;

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(tripper);

        if (playSound)
            _audio.PlayPredicted(chasm.Comp.FallingSound, chasm, tripper);

        var chasmEvent = new EntityStartedFallingIntoChasmEvent((tripper, falling));
        RaiseLocalEvent(chasm, ref chasmEvent);
        var tripperEvent = new StartedFallingIntoChasmEvent((chasm, chasm.Comp));
        RaiseLocalEvent(tripper, ref tripperEvent);

        Entity<ChasmFallingComponent> ret = (tripper, falling);
        Dirty(ret);
        return ret;
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

    private void OnShutdown(Entity<ChasmComponent> entity, ref ComponentShutdown args)
    {
        var e = EntityQueryEnumerator<ChasmFallingComponent>();
        while (e.MoveNext(out var fallingEnt, out var falling))
        {
            if (falling.FallingInto != entity.Owner)
                continue;

            RemCompDeferred<ChasmFallingComponent>(fallingEnt);
        }
    }
}
