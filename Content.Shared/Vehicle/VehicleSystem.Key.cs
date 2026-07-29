using System.Linq;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem
{
    [SubscribeLocalEvent]
    private void OnGenericKeyedInsertAttempt(Entity<GenericKeyedVehicleComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || _timing.ApplyingState || !ent.Comp.PreventInvalidInsertion || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (_entityWhitelist.IsWhitelistPass(ent.Comp.KeyWhitelist, args.EntityUid))
            return;

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnGenericKeyedEntInserted(Entity<GenericKeyedVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        RefreshCanRun((ent.Owner, vehicle));
    }

    [SubscribeLocalEvent]
    private void OnGenericKeyedEntRemoved(Entity<GenericKeyedVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        RefreshCanRun((ent.Owner, vehicle));
    }

    [SubscribeLocalEvent]
    private void OnGenericKeyedCanRun(Entity<GenericKeyedVehicleComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
        {
            args = args with { CanRun = false };
            return;
        }

        var hasKey = container.ContainedEntities.Any(contained =>
            _entityWhitelist.IsWhitelistPass(ent.Comp.KeyWhitelist, contained));

        if (!hasKey)
            args = args with { CanRun = false };
    }
}
