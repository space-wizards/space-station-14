using Content.Shared.ActionBlocker;
using Content.Shared.Chasm.Components;
using Content.Shared.Chasm.Events;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
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
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedGrapplingGunSystem _grapple = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<ChasmFallingComponent> _chasmFallingQuery;
    [Dependency] private EntityQuery<ChasmComponent> _chasmQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<ChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ChasmComponent, EntityTerminatingEvent>(OnChasmDelete);
        SubscribeLocalEvent<ChasmComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ChasmFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ChasmFallingComponent, EntityTerminatingEvent>(OnFallingDelete);
        SubscribeLocalEvent<ChasmFallingComponent, InteractHandEvent>(OnBeforeInteract);

        SubscribeLocalEvent<ChasmContainerComponent, ChasmFallEffectsEvent>(OnContainerFall);
        SubscribeLocalEvent<ChasmDeleteComponent, ChasmFallEffectsEvent>(OnDeleteFall);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextEffectsTime)
                continue;

            EndFalling((uid, chasm));
        }
    }

    /// <summary>
    /// Forces the <see cref="tripper"/> to start falling into a <see cref="chasm"/>.
    /// </summary>
    /// <param name="chasm">The target chasm entity that the tripper is falling into.</param>
    /// <param name="tripper">The entity that is falling into a chasm.</param>
    /// <param name="playSound">Controls if the chasm should play the falling sound.</param>
    [PublicAPI]
    public void StartFalling(Entity<ChasmComponent> chasm, EntityUid tripper, bool playSound = true)
    {
        var falling = AddComp<ChasmFallingComponent>(tripper);

        falling.NextEffectsTime = _timing.CurTime + falling.EffectsTime;
        falling.FallChasm = chasm;
        chasm.Comp.FallingEntities.Add(tripper);

        DirtyFields(tripper, falling, null, nameof(ChasmFallingComponent.NextEffectsTime), nameof(ChasmFallingComponent.FallChasm));
        DirtyField(chasm, chasm.Comp, nameof(ChasmComponent.FallingEntities));

        _blocker.UpdateCanMove(tripper);

        var ev = new StartChasmFallingEvent(chasm);
        RaiseLocalEvent(tripper, ref ev);

        if (playSound)
            _audio.PlayPredicted(chasm.Comp.FallingSound, chasm, tripper);
    }

    /// <summary>
    /// Immedieatly ends the falling of an entity into a chasm.
    /// </summary>
    /// <param name="tripper">The currently falling entity.</param>
    [PublicAPI]
    public void EndFalling(Entity<ChasmFallingComponent?> tripper)
    {
        if (!Resolve(tripper.Owner, ref tripper.Comp))
            return;

        var resetVisualsEv = new ResetChasmVisualsEvent();
        RaiseLocalEvent(tripper.Owner, ref resetVisualsEv);

        if (!TryComp(tripper.Comp.FallChasm, out ChasmComponent? chasmComp))
            return;

        chasmComp.FallingEntities.Remove(tripper.Owner);
        var beforeEv = new BeforeChasmFallEvent(tripper.Comp.FallChasm);
        RaiseLocalEvent(tripper.Owner, ref beforeEv);
        if (beforeEv.Cancelled)
            return;

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

        RemComp(tripper.Owner, tripper.Comp);
        _blocker.UpdateCanMove(tripper);
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

    private void OnFallingDelete(Entity<ChasmFallingComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp(ent.Comp.FallChasm, out ChasmComponent? chasm))
            chasm.FallingEntities.Remove(ent.Owner);
    }

    private void OnBeforeInteract(EntityUid uid, ChasmFallingComponent component, ref InteractHandEvent args)
    {
        args.Handled = true; // Falling entities are considered out of reach
    }

    private void OnDeleteFall(Entity<ChasmDeleteComponent> ent, ref ChasmFallEffectsEvent args)
    {
        PredictedQueueDel(args.Entity);
    }

    private void OnContainerFall(Entity<ChasmContainerComponent> ent, ref ChasmFallEffectsEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        _container.Insert(args.Entity, container);
    }
    
    private void OnShutdown(Entity<ChasmComponent> entity, ref ComponentShutdown args)
    {
        foreach (var uid in ent.Comp.FallingEntities)
        {
            if (TerminatingOrDeleted(uid) || !Exists(uid))
                continue;

            var resetVisualsEv = new ResetChasmVisualsEvent();
            RaiseLocalEvent(uid, ref resetVisualsEv);

            RemCompDeferred<ChasmFallingComponent>(uid);
        }
    }
}
