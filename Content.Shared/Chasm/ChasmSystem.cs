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
using Robust.Shared.Timing;

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
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<ChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ChasmComponent, EntityTerminatingEvent>(OnChasmDelete);

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

        var ev = new ChasmFallEffectsEvent(tripper.Owner);
        RaiseLocalEvent(tripper.Comp.FallChasm.Value, ref ev);

        RemComp(tripper.Owner, tripper.Comp);
        _blocker.UpdateCanMove(tripper);
    }

    private void OnStepTriggered(Entity<ChasmComponent> ent, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (HasComp<ChasmFallingComponent>(args.Tripper))
            return;

        StartFalling(ent, args.Tripper);
    }

    private void OnStepTriggerAttempt(Entity<ChasmComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (_grapple.IsEntityHooked(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }

    private void OnChasmDelete(Entity<ChasmComponent> ent, ref EntityTerminatingEvent args)
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

    private void OnUpdateCanMove(Entity<ChasmFallingComponent> ent, ref UpdateCanMoveEvent args)
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
}
