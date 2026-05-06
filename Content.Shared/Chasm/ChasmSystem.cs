using Content.Shared.ActionBlocker;
using Content.Shared.Chasm.Components;
using Content.Shared.Chasm.Events;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Chasm;

/// <summary>
///     Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed class ChasmSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGrapplingGunSystem _grapple = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<ChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ChasmComponent, EntityTerminatingEvent>(OnChasmDelete);

        SubscribeLocalEvent<ChasmFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ChasmFallingComponent, EntityTerminatingEvent>(OnFallingDelete);
        SubscribeLocalEvent<ChasmFallingComponent, InteractHandEvent>(OnBeforeInteract);

        SubscribeLocalEvent<ChasmContainerComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<ChasmContainerComponent, ChasmFallEffectsEvent>(OnContainerFall);
        SubscribeLocalEvent<ChasmDeleteComponent, ChasmFallEffectsEvent>(OnDeleteFall);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextDeletionTime)
                continue;

            EndFalling((uid, chasm));
        }
    }

    private void OnStepTriggered(EntityUid uid, ChasmComponent component, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (HasComp<ChasmFallingComponent>(args.Tripper))
            return;

        StartFalling(uid, component, args.Tripper);
    }

    public void StartFalling(EntityUid chasm, ChasmComponent component, EntityUid tripper, bool playSound = true)
    {
        var falling = AddComp<ChasmFallingComponent>(tripper);

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        falling.FallChasm = chasm;
        component.FallingEntities.Add(tripper);

        DirtyFields(tripper, falling, null, nameof(ChasmFallingComponent.NextDeletionTime), nameof(ChasmFallingComponent.FallChasm));
        DirtyField(chasm, component, nameof(ChasmComponent.FallingEntities));

        _blocker.UpdateCanMove(tripper);

        if (playSound)
            _audio.PlayPredicted(component.FallingSound, chasm, tripper);
    }

    public void EndFalling(Entity<ChasmFallingComponent> tripper)
    {
        RemCompDeferred(tripper.Owner, tripper.Comp);

        if (!TryComp(tripper.Comp.FallChasm, out ChasmComponent? chasmComp))
            return;

        chasmComp.FallingEntities.Remove(tripper.Owner);
        var beforeEv = new BeforeChasmFallEvent(tripper.Comp.FallChasm);
        RaiseLocalEvent(tripper.Owner, ref beforeEv);
        if (beforeEv.Cancelled)
            return;

        var ev = new ChasmFallEffectsEvent(tripper.Owner);
        RaiseLocalEvent(tripper.Comp.FallChasm.Value, ref ev);
    }

    private void OnStepTriggerAttempt(EntityUid uid, ChasmComponent component, ref StepTriggerAttemptEvent args)
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
            if (!TerminatingOrDeleted(uid) && Exists(uid))
                RemComp<ChasmFallingComponent>(uid);
        }
    }

    private void OnUpdateCanMove(EntityUid uid, ChasmFallingComponent component, UpdateCanMoveEvent args)
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
        args.Handled = true; // You can't hand interact with already falling entities
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

        if (ent.Comp.DoStun)
            EnsureComp<StunnedComponent>(args.Entity);
    }

    private void OnRemovedFromContainer(Entity<ChasmContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (ent.Comp.DoStun)
            RemComp<StunnedComponent>(args.Entity);
    }
}
