using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle;

/// <summary>
/// Handles logic relating to vehicles.
/// </summary>
public sealed partial class VehicleSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeOperator();
        InitializeKey();

        SubscribeLocalEvent<VehicleComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<VehicleComponent, UpdateCanMoveEvent>(OnVehicleUpdateCanMove);
        SubscribeLocalEvent<VehicleComponent, ComponentShutdown>(OnVehicleShutdown);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnVehicleGetAdditionalAccess);

        SubscribeLocalEvent<VehicleOperatorComponent, ComponentShutdown>(OnOperatorShutdown);
    }

    /// <remarks>
    /// We subscribe to BeforeDamageChangedEvent so that we can access the damage value before the container is applied.
    /// </remarks>
    private void OnBeforeDamageChanged(Entity<VehicleComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.TransferDamage || !args.Damage.AnyPositive() || ent.Comp.Operator is not { } operatorUid)
            return;

        var damage = DamageSpecifier.GetPositive(args.Damage);

        if (ent.Comp.TransferDamageModifier is { } modifierSet)
        {
            // Reduce damage to via the specified modifier, if provided.
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
        }

        _damageable.TryChangeDamage(operatorUid, damage, origin: args.Origin);
    }

    private void OnVehicleUpdateCanMove(Entity<VehicleComponent> ent, ref UpdateCanMoveEvent args)
    {
        var ev = new VehicleCanRunEvent(ent);
        RaiseLocalEvent(ent, ref ev);
        if (!ev.CanRun)
            args.Cancel();
    }

    private void OnVehicleShutdown(Entity<VehicleComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveOperator(ent);
    }

    private void OnVehicleGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        // Vehicles inherit access from whoever is driving them
        if (ent.Comp.Operator is { } operatorUid)
            args.Entities.Add(operatorUid);
    }

    private void OnOperatorShutdown(Entity<VehicleOperatorComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveOperator((ent, ent));
    }

    /// <summary>
    /// Set the operator for a given vehicle
    /// </summary>
    /// <param name="entity">The vehicle</param>
    /// <param name="uid">The new operator. If null, will only remove the operator.</param>
    /// <param name="removeExisting">If true, will remove the current operator when setting the new one.</param>
    /// <returns>If the new operator was successfully able to be set</returns>
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

        if (uid != null && !CanOperate(entity.AsNullable(), uid.Value))
            return false;

        var oldOperator = entity.Comp.Operator;

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
            // AddComp used for noisy fail. This should never be an issue.
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

        RefreshCanRun((entity, entity.Comp));

        var setEvent = new VehicleOperatorSetEvent(uid, oldOperator);
        RaiseLocalEvent(entity, ref setEvent);

        Dirty(entity);
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
        return TrySetOperator(entity, null, removeExisting: true);
    }

    /// <summary>
    /// From an operator, removes it from the vehicle
    /// </summary>
    /// <param name="operatorEntity">The operator who is riding a vehicle</param>
    /// <returns>If the operator was removed successfully or if the entity was not operating a vehicle.</returns>
    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleOperatorComponent?> operatorEntity)
    {
        if (!Resolve(operatorEntity, ref operatorEntity.Comp, false))
            return true;

        if (!TryComp<VehicleComponent>(operatorEntity.Comp.Vehicle, out var vehicle))
            return true;

        return TrySetOperator((operatorEntity.Comp.Vehicle.Value, vehicle), null, removeExisting: true);
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

        if (!TryComp<VehicleOperatorComponent>(operatorUid, out var operatorComponent))
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
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (_entityWhitelist.IsWhitelistFail(entity.Comp.OperatorWhitelist, uid))
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
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        if (TryComp<InputMoverComponent>(entity, out var inputMover))
        {
            _appearance.SetData(entity, VehicleVisuals.CanRun, inputMover.CanMove, appearance);
        }

        _appearance.SetData(entity, VehicleVisuals.HasOperator, entity.Comp.Operator is not null, appearance);
    }
}
