using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vehicle;

/// <summary>
/// Handles logic relating to vehicles.
/// </summary>
public sealed class VehicleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<VehicleComponent, UpdateCanMoveEvent>(OnVehicleUpdateCanMove);
        SubscribeLocalEvent<VehicleComponent, ComponentShutdown>(OnVehicleShutdown);

        SubscribeLocalEvent<VehicleOperatorComponent, ComponentShutdown>(OnOperatorShutdown);

        SubscribeLocalEvent<StrapVehicleComponent, StrappedEvent>(OnVehicleStrapped);
        SubscribeLocalEvent<StrapVehicleComponent, UnstrappedEvent>(OnVehicleUnstrapped);

        SubscribeLocalEvent<ContainerVehicleComponent, EntInsertedIntoContainerMessage>(OnContainerEntInserted);
        SubscribeLocalEvent<ContainerVehicleComponent, EntRemovedFromContainerMessage>(OnContainerEntRemoved);
    }

    /// <remarks>
    /// We subscribe to BeforeDamageChangedEvent so that we can access the damage value before the container is added.
    /// </remarks>
    private void OnBeforeDamageChanged(Entity<VehicleComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.TransferDamage || args.Damage.AnyPositive() || ent.Comp.Operator is not { } operatorUid)
            return;

        var damage = args.Damage;
        if (_prototype.TryIndex(ent.Comp.TransferDamageModifier, out var modifierSet))
        {
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
        }

        _damageable.TryChangeDamage(operatorUid, damage, origin: args.Origin);
    }

    private void OnVehicleUpdateCanMove(Entity<VehicleComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Operator is null)
            args.Cancel();
    }

    private void OnVehicleShutdown(Entity<VehicleComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveOperator(ent);
    }

    private void OnOperatorShutdown(Entity<VehicleOperatorComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveOperator(ent);
    }

    private void OnVehicleStrapped(Entity<StrapVehicleComponent> ent, ref StrappedEvent args)
    {
        if (!TryComp<VehicleComponent>(ent, out var vehicle))
            return;
        TrySetOperator((ent, vehicle), args.Buckle);
    }

    private void OnVehicleUnstrapped(Entity<StrapVehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<VehicleComponent>(ent, out var vehicle))
            return;
        TrySetOperator((ent, vehicle), null);
    }

    private void OnContainerEntInserted(Entity<ContainerVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!TryComp<VehicleComponent>(ent, out var vehicle))
            return;

        TrySetOperator((ent, vehicle), args.Entity, removeExisting: false);
    }

    private void OnContainerEntRemoved(Entity<ContainerVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;
        if (!TryComp<VehicleComponent>(ent, out var vehicle))
            return;
        if (vehicle.Operator != args.Entity)
            return;

        TryRemoveOperator((ent, vehicle));
    }

    public bool TrySetOperator(Entity<VehicleComponent> entity, EntityUid? uid, bool removeExisting = true)
    {
        if (entity.Comp.Operator == null && uid is null)
            return false;

        // Do not run logic if the entity is already operating a vehicle.
        // However, if they are operating *this* vehicle, return true (they are indeed the operator)
        if (TryComp<VehicleOperatorComponent>(uid, out var eOperator))
            return eOperator.Vehicle == entity.Owner;

        if (!removeExisting && entity.Comp.Operator is not null)
            return false;

        if (uid != null && !CanOperate(entity, uid.Value))
            return false;

        if (entity.Comp.Operator is { } currentOperator && TryComp<VehicleOperatorComponent>(currentOperator, out var currentOperatorComponent))
        {
            var exitEvent = new OnVehicleExitedEvent(entity, currentOperator);
            RaiseLocalEvent(currentOperator, ref exitEvent);

            currentOperatorComponent.Vehicle = null;
            RemCompDeferred<VehicleOperatorComponent>(currentOperator);
            RemCompDeferred<RelayInputMoverComponent>(currentOperator);
        }

        entity.Comp.Operator = uid;

        if (uid != null)
        {
            var vehicleOperator = AddComp<VehicleOperatorComponent>(uid.Value);
            vehicleOperator.Vehicle = entity.Owner;
            Dirty(uid.Value, vehicleOperator);

            _mover.SetRelay(uid.Value, entity);

            var enterEvent = new OnVehicleEnteredEvent(entity, uid.Value);
            RaiseLocalEvent(uid.Value, ref enterEvent);
        }
        else
        {
            RemCompDeferred<MovementRelayTargetComponent>(entity);
        }

        _actionBlocker.UpdateCanMove(entity);
        UpdateAppearance(entity);

        var setEvent = new VehicleOperatorSetEvent(uid);
        RaiseLocalEvent(entity, ref setEvent);

        Dirty(entity);
        return true;
    }

    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleComponent> entity)
    {
        return TrySetOperator(entity, null);
    }

    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleOperatorComponent> operatorEntity)
    {
        if (!TryComp<VehicleComponent>(operatorEntity.Comp.Vehicle, out var vehicle))
            return false;

        return TrySetOperator((operatorEntity.Comp.Vehicle.Value, vehicle), null);
    }

    [PublicAPI]
    public bool TryGetOperator(Entity<VehicleComponent?> entity, [NotNullWhen(true)] out Entity<VehicleOperatorComponent>? operatorEnt)
    {
        operatorEnt = null;
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (entity.Comp.Operator is not { } operatorUid)
            return false;

        if (!TryComp<VehicleOperatorComponent>(operatorUid, out var operatorComponent))
            return false;

        operatorEnt = (operatorUid, operatorComponent);
        return true;
    }

    public bool CanOperate(Entity<VehicleComponent> entity, EntityUid uid)
    {
        if (_entityWhitelist.IsWhitelistFail(entity.Comp.OperatorWhitelist, uid))
            return false;

        return true;
    }

    private void UpdateAppearance(Entity<VehicleComponent> entity)
    {
        var appearance = CompOrNull<AppearanceComponent>(entity);

        if (TryComp<InputMoverComponent>(entity, out var inputMover))
        {
            _appearance.SetData(entity, VehicleVisuals.CanRun, inputMover.CanMove, appearance);
        }

        _appearance.SetData(entity, VehicleVisuals.HasOperator, entity.Comp.Operator is not null, appearance);
    }
}
