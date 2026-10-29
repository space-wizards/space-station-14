using System.Diagnostics.CodeAnalysis;
using Content.Shared.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle.Systems;

public sealed partial class VehicleSystem
{
    [SubscribeLocalEvent]
    private void OnVehicleStrapped(Entity<StrapVehicleComponent> ent, ref StrappedEvent args)
    {
        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        TrySetOperator((ent, vehicle), args.Buckle);
    }

    [SubscribeLocalEvent]
    private void OnVehicleUnstrapped(Entity<StrapVehicleComponent> ent, ref UnstrappedEvent args)
    {
        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        if (vehicle.Operator != args.Buckle)
            return;

        TryRemoveOperator((ent, vehicle));
    }

    [SubscribeLocalEvent]
    private void OnContainerEntInserted(Entity<ContainerVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        TrySetOperator((ent, vehicle), args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnContainerEntRemoved(Entity<ContainerVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        if (vehicle.Operator != args.Entity)
            return;

        TryRemoveOperator((ent, vehicle));
    }

    /// <summary>
    /// Attempts to get the container configured for a container vehicle's operator.
    /// </summary>
    public bool TryGetOperatorContainer(
        Entity<ContainerVehicleComponent?> vehicle,
        [NotNullWhen(true)] out BaseContainer? container)
    {
        container = null;
        return Resolve(vehicle, ref vehicle.Comp, false) &&
            _container.TryGetContainer(vehicle, vehicle.Comp.ContainerId, out container);
    }

    /// <summary>
    /// Checks whether an entity can enter a container vehicle.
    /// </summary>
    /// <remarks>
    /// An entity becoming the operator must be able to operate the vehicle.
    /// Additional occupants do not need to be eligible operators.
    /// </remarks>
    public bool CanEnter(Entity<VehicleComponent?> vehicle, EntityUid toEnter)
    {
        if (!Resolve(vehicle, ref vehicle.Comp, false))
            return false;

        return CanEnterContainer(vehicle, toEnter) &&
               (HasOperator(vehicle) || CanOperate(vehicle, toEnter));
    }

    /// <summary>
    /// Checks whether an entity can physically enter a container vehicle without checking operator eligibility.
    /// </summary>
    private bool CanEnterContainer(Entity<VehicleComponent?> vehicle, EntityUid toEnter)
    {
        if (!Resolve(vehicle, ref vehicle.Comp, false))
            return false;

        if (!_actionBlocker.CanMove(toEnter))
            return false;

        if (GetOperatorOrNull(vehicle) == toEnter)
            return false;

        return TryGetOperatorContainer(vehicle.Owner, out var container) &&
               _container.CanInsert(toEnter, container);
    }

    /// <summary>
    /// Attempts to insert an entity into a container vehicle.
    /// </summary>
    public bool TryEnter(Entity<VehicleComponent?> vehicle, EntityUid toEnter)
    {
        if (!CanEnter(vehicle, toEnter))
            return false;

        if (!TryGetOperatorContainer(vehicle.Owner, out var container))
            return false;

        return _container.Insert(toEnter, container);
    }

    /// <summary>
    /// Attempts to remove the current operator from a container vehicle.
    /// </summary>
    public bool TryExit(Entity<VehicleComponent?> vehicle)
    {
        if (!TryGetOperator(vehicle, out var operatorEnt))
            return false;

        if (!TryGetOperatorContainer(vehicle.Owner, out var container))
            return false;

        return _container.Remove(operatorEnt.Value.Owner, container);
    }
}
