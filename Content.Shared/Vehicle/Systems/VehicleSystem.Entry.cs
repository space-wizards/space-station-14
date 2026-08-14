using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Vehicle.Components;

namespace Content.Shared.Vehicle.Systems;

public sealed partial class VehicleSystem
{
    [SubscribeLocalEvent]
    private void OnCanDragDrop(Entity<ContainerVehicleEntryComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.CanDrop |= CanEnterViaInteraction(ent.Owner, args.Dragged);
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<ContainerVehicleEntryComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!CanEnterViaInteraction(ent.Owner, args.Dragged))
            return;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.Dragged,
            ent.Comp.EntryDelay,
            new ContainerVehicleEntryEvent(),
            ent.Owner,
            target: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    [SubscribeLocalEvent]
    private void OnEntryCompleted(EntityUid uid, ContainerVehicleEntryComponent component, ContainerVehicleEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<ContainerVehicleComponent>(uid, out var containerVehicle))
            return;

        if (!HasOperator(uid) &&
            !CanOperate(uid, args.User))
        {
            var denied = new ContainerVehicleEntryOperatorDeniedEvent(args.User);
            RaiseLocalEvent(uid, denied);
            return;
        }

        if (!CanEnterViaInteraction(uid, args.User) ||
            !TryEnter((uid, containerVehicle), args.User))
            return;

        args.Handled = true;
    }

    private bool CanEnterViaInteraction(EntityUid vehicle, EntityUid entering)
    {
        if (!TryComp<ContainerVehicleComponent>(vehicle, out var containerVehicle) ||
            !CanEnter((vehicle, containerVehicle), entering))
            return false;

        var attempt = new ContainerVehicleEntryAttemptEvent(entering);
        RaiseLocalEvent(vehicle, attempt);
        return !attempt.Cancelled;
    }
}
