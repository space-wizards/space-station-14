using Content.Shared.ActionBlocker;
using Content.Shared.Chasm.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
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
    /// Causes <paramref name="tripper"/> to fall into <paramref name="chasm"/>: starts a falling animation, optionally
    /// plays a sound, and eventually deletes <paramref name="tripper"/>.
    /// If <paramref name="chasm"/> does not have a <see cref="ChasmComponent"/> component, does nothing and returns null.
    /// </summary>
    /// <returns>
    /// <paramref name="tripper"/> with its new <see cref="ChasmFallingComponent"/>, if the entity did start falling. Null otherwise.
    /// </returns>
    [PublicAPI]
    public Entity<ChasmFallingComponent>? StartFalling(Entity<ChasmComponent?> chasm, EntityUid tripper, bool playSound = true)
    {
        if (!_chasmQuery.Resolve(chasm, ref chasm.Comp, logMissing: false))
            return null;

        var falling = AddComp<ChasmFallingComponent>(tripper);
        falling.FallingInto = chasm;
        falling.NextEffectsTime = _timing.CurTime + falling.EffectsTime;
        chasm.Comp.FallingEntities.Add(tripper);

        _blocker.UpdateCanMove(tripper);

        if (playSound)
            _audio.PlayPredicted(chasm.Comp.FallingSound, chasm, tripper);

        Entity<ChasmFallingComponent> ret = (tripper, falling);

        var chasmEvent = new EntityStartedFallingIntoChasmEvent(ret);
        RaiseLocalEvent(chasm, ref chasmEvent);
        var tripperEvent = new StartedFallingIntoChasmEvent(chasm!);
        RaiseLocalEvent(tripper, ref tripperEvent);

        DirtyFields(ret.AsNullable(), null, nameof(ChasmFallingComponent.NextEffectsTime), nameof(ChasmFallingComponent.FallingInto));
        DirtyField(chasm, chasm.Comp, nameof(ChasmComponent.FallingEntities));
        return ret;
    }

    /// <summary>
    /// Immediately ends the falling of an entity into a chasm.
    /// </summary>
    /// <param name="tripper">The currently falling entity.</param>
    [PublicAPI]
    public void EndFalling(Entity<ChasmFallingComponent?> tripper)
    {
        if (!_chasmFallingQuery.Resolve(tripper.Owner, ref tripper.Comp, logMissing: false))
            return;

        var chasm = tripper.Comp.FallingInto;

        var resetVisualsEv = new ResetChasmVisualsEvent();
        RaiseLocalEvent(tripper.Owner, ref resetVisualsEv);

        var beforeEv = new BeforeChasmFallEvent(chasm);
        RaiseLocalEvent(tripper.Owner, ref beforeEv);
        if (beforeEv.Cancelled)
            return;

        var chasmEvent = new EntityCompletedFallingIntoChasmEvent(tripper!);
        RaiseLocalEvent(chasm, ref chasmEvent);
        if (_chasmQuery.TryComp(chasm, out var chasmComp))
        {
            chasmComp.FallingEntities.Remove(tripper.Owner);
            var tripperEvent = new CompletedFallingIntoChasmEvent((chasm, chasmComp));
            RaiseLocalEvent(tripper, ref tripperEvent);
        }
        else
        {
            DebugTools.Assert($"{ToPrettyString(chasm)} is missing {nameof(ChasmComponent)} when an entity fell into it!");
        }

        var effectsEv = new ChasmFallEffectsEvent(tripper.Owner);
        RaiseLocalEvent(chasm, ref effectsEv);
        DebugTools.Assert(effectsEv.Handled, $"{ToPrettyString(chasm)} didn't handle the {nameof(ChasmFallEffectsEvent)}. Ensure that it has any component that handles the effects of falling into a chasm in the YAML.");

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
        if (_chasmQuery.TryComp(ent.Comp.FallingInto, out var chasm))
            chasm.FallingEntities.Remove(ent.Owner);
    }

    private void OnBeforeInteract(EntityUid uid, ChasmFallingComponent component, ref InteractHandEvent args)
    {
        args.Handled = true; // Falling entities are considered out of reach
    }

    private void OnDeleteFall(Entity<ChasmDeleteComponent> ent, ref ChasmFallEffectsEvent args)
    {
        PredictedQueueDel(args.Faller);
        args.Handled = true;
    }

    private void OnContainerFall(Entity<ChasmContainerComponent> ent, ref ChasmFallEffectsEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        _container.Insert(args.Faller, container);
        args.Handled = true;
    }

    private void OnShutdown(Entity<ChasmComponent> entity, ref ComponentShutdown args)
    {
        foreach (var uid in entity.Comp.FallingEntities)
        {
            if (TerminatingOrDeleted(uid) || !Exists(uid))
                continue;

            var resetVisualsEv = new ResetChasmVisualsEvent();
            RaiseLocalEvent(uid, ref resetVisualsEv);

            RemCompDeferred<ChasmFallingComponent>(uid);
            _blocker.UpdateCanMove(uid);
        }
    }
}
