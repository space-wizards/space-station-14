using Content.Shared.Botany.Items.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Medical.BiomassReclaimer;

public abstract partial class BiomassReclaimerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] protected SharedPopupSystem _popup = default!;
    [Dependency] protected SharedPowerReceiverSystem _powerReceiver = default!;

    [Dependency] protected EntityQuery<PhysicsComponent> _physicsQuery;
    [Dependency] protected EntityQuery<TransformComponent> _transformQuery;
    [Dependency] protected EntityQuery<ProduceComponent> _produceQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;

    [SubscribeLocalEvent]
    private void OnCanDrop(Entity<BiomassReclaimerComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = ValidateInsertion(ent, args.Dragged) == BiomassReclaimerInsertResult.Success;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnAfterInteractUsing(Entity<BiomassReclaimerComponent> reclaimer, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryStartInsertion(reclaimer, args.User, args.Used, true);
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<BiomassReclaimerComponent> reclaimer, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartInsertion(reclaimer, args.User, args.Dragged, false);
    }

    private bool TryStartInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid user, EntityUid toProcess, bool needHand)
    {
        if (!TryValidateInsertionAndPopup(reclaimer, toProcess, user) ||
            !_physicsQuery.TryComp(toProcess, out var physics))
            return false;

        var delay = reclaimer.Comp.BaseInsertionDelay * physics.FixturesMass;
        return _doAfterSystem.TryStartDoAfter(
            new DoAfterArgs(EntityManager, user, delay, new ReclaimerDoAfterEvent(), reclaimer, reclaimer, toProcess)
            {
                NeedHand = needHand,
                BreakOnMove = true
            });
    }

    protected bool TryValidateInsertionAndPopup(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target, EntityUid user)
    {
        var result = ValidateInsertion(reclaimer, target);
        if (GetInsertionFailureLoc(result) is not { } message)
            return true;

        _popup.PopupEntity(Loc.GetString(message), reclaimer, user);
        return false;
    }

    private static LocId? GetInsertionFailureLoc(BiomassReclaimerInsertResult result)
    {
        return result switch
        {
            BiomassReclaimerInsertResult.Success => null,
            BiomassReclaimerInsertResult.InvalidTarget => "biomass-reclaimer-invalid-target",
            BiomassReclaimerInsertResult.Unanchored => "biomass-reclaimer-unanchored",
            BiomassReclaimerInsertResult.Unpowered => "biomass-reclaimer-unpowered",
            BiomassReclaimerInsertResult.TargetAlive => "biomass-reclaimer-safety-enabled",
            BiomassReclaimerInsertResult.Busy => "biomass-reclaimer-busy",
            BiomassReclaimerInsertResult.SoulPresent => "biomass-reclaimer-soul-present",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }

    protected virtual BiomassReclaimerInsertResult ValidateInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target)
    {
        var isPlant = _produceQuery.HasComp(target);
        if ((!isPlant && !_mobStateQuery.HasComp(target)) || !_physicsQuery.HasComp(target))
            return BiomassReclaimerInsertResult.InvalidTarget;

        if (!_transformQuery.GetComponent(reclaimer).Anchored)
            return BiomassReclaimerInsertResult.Unanchored;

        if (!_powerReceiver.IsPowered(reclaimer.Owner))
            return BiomassReclaimerInsertResult.Unpowered;

        return isPlant || !reclaimer.Comp.SafetyEnabled || _mobState.IsDead(target)
            ? BiomassReclaimerInsertResult.Success
            : BiomassReclaimerInsertResult.TargetAlive;
    }
}
