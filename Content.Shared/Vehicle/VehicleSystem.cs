using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Vehicle;

/// <summary>
/// Handles logic relating to vehicles.
/// </summary>
public sealed partial class VehicleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<VehicleComponent> _vehicleQuery;
    [Dependency] private EntityQuery<VehicleOperatorComponent> _operatorQuery;
    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;
    [Dependency] private EntityQuery<InputMoverComponent> _inputMoverQuery;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery;

    /// <remarks>
    /// We subscribe to BeforeDamageChangedEvent so that we can access the damage value before the container is applied.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnBeforeDamageChanged(Entity<VehicleComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.TransferDamage || !args.Damage.AnyPositive() || ent.Comp.Operator is not { } operatorUid)
            return;

        var damage = DamageSpecifier.GetPositive(args.Damage);

        if (ent.Comp.TransferDamageModifier is { } modifierSet)
        {
            // Reduce damage to the operator via the specified modifier, if provided.
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
        }

        _damageable.TryChangeDamage(operatorUid, damage, origin: args.Origin);
    }

    [SubscribeLocalEvent]
    private void OnVehicleUpdateCanMove(Entity<VehicleComponent> ent, ref UpdateCanMoveEvent args)
    {
        var ev = new VehicleCanRunEvent(ent);
        RaiseLocalEvent(ent, ref ev);
        if (!ev.CanRun)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnOperatorUpdateCanMove(Entity<VehicleOperatorComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Vehicle is not { } vehicleUid ||
            !_vehicleQuery.TryComp(vehicleUid, out var vehicle))
            return;

        if (!CanOperate((vehicleUid, vehicle), ent.Owner))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnVehicleShutdown(Entity<VehicleComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        TryRemoveOperator(ent);
    }

    [SubscribeLocalEvent]
    private void OnVehicleGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        // Vehicles inherit access from whoever is driving them
        if (ent.Comp.Operator is { } operatorUid)
            args.Entities.Add(operatorUid);
    }

    [SubscribeLocalEvent]
    private void OnOperatorShutdown(Entity<VehicleOperatorComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        TryRemoveOperator((ent, ent));
    }

    /// <summary>
    /// Set the operator for a given vehicle
    /// </summary>
    /// <param name="entity">The vehicle</param>
    /// <param name="operatorUid">The new operator.</param>
    /// <returns>If the new operator was successfully able to be set</returns>
    public bool TrySetOperator(Entity<VehicleComponent> entity, EntityUid operatorUid)
    {
        // Early exit if setting the same operator that's already present.
        if (entity.Comp.Operator == operatorUid)
            return _operatorQuery.TryComp(operatorUid, out var existingOperator) && existingOperator.Vehicle == entity.Owner;

        if (entity.Comp.Operator is not null)
            return false;

        if (_operatorQuery.TryComp(operatorUid, out var eOperator) && eOperator.Vehicle is not null)
            return false;

        if (!CanOperate(entity.AsNullable(), operatorUid))
            return false;

        entity.Comp.Operator = operatorUid;

        if (_operatorQuery.HasComp(operatorUid))
        {
            var vehicleOperator = Comp<VehicleOperatorComponent>(operatorUid);
            vehicleOperator.Vehicle = entity.Owner;
            Dirty(operatorUid, vehicleOperator);
        }
        else
        {
            var vehicleOperator = AddComp<VehicleOperatorComponent>(operatorUid);
            vehicleOperator.Vehicle = entity.Owner;
            Dirty(operatorUid, vehicleOperator);
        }

        _mover.SetRelay(operatorUid, entity);

        var enterEvent = new OnVehicleEnteredEvent(entity, operatorUid);
        RaiseLocalEvent(operatorUid, ref enterEvent);

        RefreshCanRun((entity, entity.Comp));

        var setEvent = new VehicleOperatorSetEvent(operatorUid, null);
        RaiseLocalEvent(entity, ref setEvent);

        DirtyFields(entity.Owner, entity.Comp, null, nameof(VehicleComponent.Operator));
        return true;
    }

    /// <summary>
    /// Attempts to remove the current operator from a vehicle
    /// </summary>
    /// <param name="entity">The vehicle whose operator is being removed.</param>
    /// <returns>If the operator was removed successfully</returns>
    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleComponent> entity)
    {
        if (entity.Comp.Operator is not { } currentOperator)
            return false;

        _operatorQuery.TryComp(currentOperator, out var currentOperatorComponent);

        if (currentOperatorComponent != null)
        {
            var exitEvent = new OnVehicleExitedEvent(entity, currentOperator);
            RaiseLocalEvent(currentOperator, ref exitEvent);

            currentOperatorComponent.Vehicle = null;
            RemCompDeferred<VehicleOperatorComponent>(currentOperator);
        }

        entity.Comp.Operator = null;
        ClearOperatorRelays(currentOperator, entity);

        RefreshCanRun((entity, entity.Comp));

        var setEvent = new VehicleOperatorSetEvent(null, currentOperator);
        RaiseLocalEvent(entity, ref setEvent);

        Dirty(entity);
        return true;
    }

    private void ClearOperatorRelays(EntityUid operatorUid, EntityUid vehicleUid)
    {
        if (TryComp<RelayInputMoverComponent>(operatorUid, out var relayMover) &&
            relayMover.RelayEntity == vehicleUid)
        {
            RemCompDeferred<RelayInputMoverComponent>(operatorUid);
        }

        if (TryComp<InteractionRelayComponent>(operatorUid, out var interactionRelay) &&
            interactionRelay.RelayEntity == vehicleUid)
        {
            RemCompDeferred<InteractionRelayComponent>(operatorUid);
        }

        if (TryComp<MovementRelayTargetComponent>(vehicleUid, out var relayTarget) &&
            relayTarget.Source == operatorUid)
        {
            RemCompDeferred<MovementRelayTargetComponent>(vehicleUid);
        }
    }

    /// <summary>
    /// From an operator, removes it from the vehicle
    /// </summary>
    /// <param name="operatorEntity">The operator who is riding a vehicle</param>
    /// <returns>If the operator was removed successfully, or if the entity was not operating a vehicle.</returns>
    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleOperatorComponent?> operatorEntity)
    {
        if (!Resolve(operatorEntity, ref operatorEntity.Comp, false))
            return true;

        var vehicleUid = operatorEntity.Comp.Vehicle;
        if (vehicleUid is null)
            return true;

        if (_vehicleQuery.TryComp(vehicleUid, out var vehicle))
            return TryRemoveOperator((vehicleUid.Value, vehicle));

        ClearOperatorRelays(operatorEntity.Owner, vehicleUid.Value);
        operatorEntity.Comp.Vehicle = null;
        RemCompDeferred<VehicleOperatorComponent>(operatorEntity.Owner);
        return true;
    }

    /// <summary>
    /// Attempts to get the current operator of a vehicle
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="operatorEnt"></param>
    [PublicAPI]
    public bool TryGetOperator(Entity<VehicleComponent?> entity, [NotNullWhen(true)] out Entity<VehicleOperatorComponent>? operatorEnt)
    {
        operatorEnt = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (entity.Comp.Operator is not { } operatorUid)
            return false;

        if (!_operatorQuery.TryComp(operatorUid, out var operatorComponent))
            return false;

        operatorEnt = (operatorUid, operatorComponent);
        return true;
    }

    /// <summary>
    /// Returns the operator of the vehicle or none if there isn't one present
    /// </summary>
    public EntityUid? GetOperatorOrNull(Entity<VehicleComponent?> entity)
    {
        TryGetOperator(entity, out var operatorEnt);
        return operatorEnt;
    }

    /// <summary>
    /// Checks if the current vehicle has an operator.
    /// </summary>
    [PublicAPI]
    public bool HasOperator(Entity<VehicleComponent?> entity)
    {
        return TryGetOperator(entity, out _);
    }

    /// <summary>
    /// Checks if a given entity is capable of operating a vehicle.
    /// Note that the general ability for a vehicle to run (keys, fuel, etc.) is not checked here.
    /// This is *only* for checks on the user.
    /// </summary>
    public bool CanOperate(Entity<VehicleComponent?> entity, EntityUid uid)
    {
        if (!Exists(uid))
            return false;

        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (_entityWhitelist.IsWhitelistFail(entity.Comp.OperatorWhitelist, uid))
            return false;

        if (entity.Comp.RequiresHands && (!_handsQuery.HasComp(uid) || !_actionBlocker.CanInteract(uid, entity)))
            return false;

        return _actionBlocker.CanConsciouslyPerformAction(uid);
    }

    /// <summary>
    /// Checks if the vehicle is capable of running (has keys, fuel, etc.) and caches the value.
    /// Updates the appearance data.
    /// </summary>
    public void RefreshCanRun(Entity<VehicleComponent?> entity)
    {
        if (TerminatingOrDeleted(entity))
            return;

        if (!Resolve(entity, ref entity.Comp))
            return;

        _actionBlocker.UpdateCanMove(entity);
        UpdateAppearance((entity, entity.Comp));
    }

    private void UpdateAppearance(Entity<VehicleComponent> entity)
    {
        if (!_appearanceQuery.TryComp(entity, out var appearance))
            return;

        if (_inputMoverQuery.TryComp(entity, out var inputMover))
            _appearance.SetData(entity, VehicleVisuals.CanRun, inputMover.CanMove, appearance);

        _appearance.SetData(entity, VehicleVisuals.HasOperator, entity.Comp.Operator is not null, appearance);
    }
}
