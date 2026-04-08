using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem
{
    private void InitializeKey()
    {
        SubscribeLocalEvent<GenericKeyedVehicleComponent, ContainerIsInsertingAttemptEvent>(OnGenericKeyedInsertAttempt);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, EntInsertedIntoContainerMessage>(OnGenericKeyedEntInserted);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, EntRemovedFromContainerMessage>(OnGenericKeyedEntRemoved);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, VehicleCanRunEvent>(OnGenericKeyedCanRun);
    }

    private void OnGenericKeyedInsertAttempt(Entity<GenericKeyedVehicleComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || _timing.ApplyingState || !ent.Comp.PreventInvalidInsertion || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (_entityWhitelist.IsWhitelistPass(ent.Comp.KeyWhitelist, args.EntityUid))
            return;

        args.Cancel();
    }

    private void OnGenericKeyedEntInserted(Entity<GenericKeyedVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;
        RefreshCanRun(ent.Owner);
    }

    private void OnGenericKeyedEntRemoved(Entity<GenericKeyedVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;
        RefreshCanRun(ent.Owner);
    }

    private void OnGenericKeyedCanRun(Entity<GenericKeyedVehicleComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
        {
            args.CanRun = false;
            return;
        }

        var hasKey = false;
        foreach (var contained in container.ContainedEntities)
        {
            if (_entityWhitelist.IsWhitelistFail(ent.Comp.KeyWhitelist, contained))
                continue;

            hasKey = true;
            break;
        }

        if (!hasKey)
            args.CanRun = false;
    }
}
