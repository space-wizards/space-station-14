using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.IV;

public sealed class IVSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJointSystem _joint = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IVSourceComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<IVSourceComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<IVSourceComponent, DragDropDraggedEvent>(OnDragDropDragged);

        SubscribeLocalEvent<IVSourceComponent, IVConnectDoAfterEvent>(OnConnectDoAfter);
        SubscribeLocalEvent<IVSourceComponent, IVDisconnectDoAfterEvent>(OnDisconnectDoAfter);

        SubscribeLocalEvent<IVSourceComponent, GetVerbsEvent<Verb>>(OnSourceGetVerbs);
        SubscribeLocalEvent<IVTargetComponent, GetVerbsEvent<Verb>>(OnTargetGetVerbs);

        SubscribeLocalEvent<IVSourceComponent, ComponentShutdown>(OnSourceShutdown);
        SubscribeLocalEvent<IVTargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<IVSourceComponent>();
        while (query.MoveNext(out var sourceUid, out var sourceComp))
        {
            if (sourceComp.IVTarget is not { } target)
                continue;

            if (sourceComp.NextUpdate > _timing.CurTime)
                continue;

            sourceComp.NextUpdate = _timing.CurTime + sourceComp.UpdateInterval;
            Dirty(sourceUid, sourceComp);

            if (!TryComp<BloodstreamComponent>(target, out var bloodstream) || !TryComp<IVTargetComponent>(target, out var targetComp))
                continue;

            TickIV((sourceUid, sourceComp), (target, targetComp, bloodstream));
        }
    }

    private void OnCanDrag(Entity<IVSourceComponent> ent, ref CanDragEvent args)
    {
        args.Handled = ent.Comp.IVTarget is null;
    }

    private void OnCanDropDragged(Entity<IVSourceComponent> ent, ref CanDropDraggedEvent args)
    {
        if (!TryComp<IVTargetComponent>(args.Target, out var target))
            return;

        args.Handled = true;
        args.CanDrop = target.IVSource is null;
    }

    private void OnDragDropDragged(Entity<IVSourceComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!TryComp<IVTargetComponent>(args.Target, out var target) || target.IVSource is not null)
            return;

        args.Handled = true;
        TryStartIV(ent.AsNullable(), (args.Target, target), args.User);
    }

    private void OnConnectDoAfter(Entity<IVSourceComponent> ent, ref IVConnectDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        StartIV(ent.AsNullable(), target, args.Args.User);
    }

    private void OnDisconnectDoAfter(Entity<IVSourceComponent> ent, ref IVDisconnectDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        StopIV(ent.AsNullable(), target, args.Args.User);
    }

    private void TickIV(Entity<IVSourceComponent> source, Entity<IVTargetComponent, BloodstreamComponent> target)
    {
        if (_itemSlots.GetItemOrNull(source, source.Comp.SlotName) is not { } contained)
            return;

        if (!_solutionContainer.TryGetDrawableSolution(contained, out var solutionEntity, out var solution))
            return;

        if (!_solutionContainer.ResolveSolution(target.Owner, target.Comp2.ChemicalSolutionName, ref target.Comp2.ChemicalSolution, out var chemsSolution))
            return;

        if (!_solutionContainer.ResolveSolution(target.Owner, target.Comp2.BloodSolutionName, ref target.Comp2.BloodSolution, out var bloodSolution))
            return;

        var bloodTransferAmount = FixedPoint2.Min(source.Comp.BloodTransferRate, bloodSolution.AvailableVolume);
        var chemsTransferAmount = FixedPoint2.Min(source.Comp.OtherTransferRate, chemsSolution.AvailableVolume);

        if (bloodTransferAmount > 0)
        {
            solution.SplitSolutionWithOnly(bloodTransferAmount, target.Comp2.BloodReagent);

            _bloodstream.TryModifyBloodLevel((target.Owner, target.Comp2), bloodTransferAmount);
        }

        if (chemsTransferAmount > 0)
        {
            var medicineSolution = solution.SplitSolutionWithout(chemsTransferAmount, target.Comp2.BloodReagent);

            if (!chemsSolution.HasOverlapAtLeast(medicineSolution, source.Comp.OtherTransferRate * 2))
            {
                _bloodstream.TryAddToChemicals((target.Owner, target.Comp2), medicineSolution);
            }
        }

        _solutionContainer.UpdateChemicals(solutionEntity.Value);
    }

    private void SetLock(Entity<IVSourceComponent> ent, bool locked)
    {
        _itemSlots.SetLock(ent, ent.Comp.SlotName, locked);
        _appearance.SetData(ent.Owner, IVSourceVisuals.HasTarget, locked);
    }

    private void StartIV(Entity<IVSourceComponent?> source, Entity<IVTargetComponent?> target, EntityUid user)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;
        if (!TryComp<PhysicsComponent>(source, out var sourcePhysics))
            return;
        if (!TryComp<PhysicsComponent>(target, out var targetPhysics))
            return;

        _popup.PopupPredicted(
            Loc.GetString(source.Comp.ConnectedUser, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            Loc.GetString(source.Comp.ConnectedOthers, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            target,
            user
        );

        target.Comp.IVSource = source;
        source.Comp.IVTarget = target;

        target.Comp.IVJointID = $"iv-joint-{GetNetEntity(target)}";

        if (!_timing.ApplyingState)
        {
            var joint = _joint.CreateDistanceJoint(target, source,
                    sourcePhysics.LocalCenter, targetPhysics.LocalCenter,
                    id: target.Comp.IVJointID);

            joint.MaxLength = joint.Length + 0.2f;
            joint.MinLength = 0f;
            joint.Stiffness = 0f;
        }

        SetLock((source, source.Comp), true);
        Dirty(target);
        Dirty(source);
    }

    private void StopIV(Entity<IVSourceComponent?> source, Entity<IVTargetComponent?> target, EntityUid user)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        _popup.PopupPredicted(
            Loc.GetString(source.Comp.DisconnectedUser, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            Loc.GetString(source.Comp.DisconnectedOthers, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            target,
            user
        );

        source.Comp.IVTarget = null;

        if (target.Comp.IVJointID is { } joint)
            _joint.RemoveJoint(target, joint);

        target.Comp.IVSource = null;
        target.Comp.IVJointID = null;

        SetLock((source, source.Comp), false);
        Dirty(source);
        Dirty(target);
    }

    private void TryStartIV(Entity<IVSourceComponent?> source, Entity<IVTargetComponent?> target, EntityUid user)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp) || source.Comp.IVTarget is not null || target.Comp.IVSource is not null)
            return;

        if (_itemSlots.GetItemOrNull(source, source.Comp.SlotName) is not { } contained)
        {
            _popup.PopupPredictedCursor(Loc.GetString(source.Comp.NoBagInserted), user);
            return;
        }

        _popup.PopupPredicted(
            Loc.GetString(source.Comp.StartConnectionUser, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            Loc.GetString(source.Comp.StartConnectionOthers, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            target,
            user
        );

        var args =
            new DoAfterArgs(EntityManager, user, source.Comp.Delay, new IVConnectDoAfterEvent(), source, target: target)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(args);
    }

    private void TryStopIV(Entity<IVSourceComponent?> source, Entity<IVTargetComponent?> target, EntityUid user)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp) || source.Comp.IVTarget is null || target.Comp.IVSource is null || source.Comp.IVTarget != target || target.Comp.IVSource != source)
            return;

        _popup.PopupPredicted(
            Loc.GetString(source.Comp.StartDisconnectionUser, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            Loc.GetString(source.Comp.StartDisconnectionOthers, ("target", Identity.Entity(target, EntityManager)), ("source", Identity.Entity(source, EntityManager)), ("user", Identity.Entity(user, EntityManager))),
            target,
            user
        );

        var args =
            new DoAfterArgs(EntityManager, user, source.Comp.Delay, new IVDisconnectDoAfterEvent(), source, target: target)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnSourceShutdown(Entity<IVSourceComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.IVTarget is not { } target || !TryComp<IVTargetComponent>(target, out var targetComp))
            return;

        if (targetComp.IVJointID is { } joint)
            _joint.RemoveJoint(target, joint);

        targetComp.IVSource = null;
        targetComp.IVJointID = null;
        SetLock(ent, false);
        Dirty(target, targetComp);
    }

    private void OnTargetShutdown(Entity<IVTargetComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.IVSource is not { } source || !TryComp<IVSourceComponent>(source, out var sourceComp))
            return;

        if (ent.Comp.IVJointID is { } joint)
            _joint.RemoveJoint(ent, joint);

        sourceComp.IVTarget = null;
        SetLock((source, sourceComp), false);
        Dirty(source, sourceComp);
    }

    private void OnSourceGetVerbs(Entity<IVSourceComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.IVTarget is not { } target)
            return;

        var user = args.User;
        Verb verb = new()
        {
            Text = Loc.GetString("verb-remove-iv"),
            Act = () => TryStopIV(ent.AsNullable(), target, user)
        };

        args.Verbs.Add(verb);
    }

    private void OnTargetGetVerbs(Entity<IVTargetComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.IVSource is not { } source)
            return;

        var user = args.User;
        Verb verb = new()
        {
            Text = Loc.GetString("verb-remove-iv"),
            Act = () => TryStopIV(source, ent.AsNullable(), user)
        };

        args.Verbs.Add(verb);
    }
}
