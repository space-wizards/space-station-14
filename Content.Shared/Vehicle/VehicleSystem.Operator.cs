using Content.Shared.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle;

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
}
