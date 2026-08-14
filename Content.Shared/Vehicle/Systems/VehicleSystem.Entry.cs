using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;

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
        StartEntryInteraction(ent, args.Dragged);
    }

    [SubscribeLocalEvent]
    private void OnAlternativeVerb(Entity<ContainerVehicleEntryComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess ||
            !args.CanInteract ||
            !CanEnterViaInteraction(ent.Owner, args.User))
            return;

        var entering = args.User;
        var enterVerb = new AlternativeVerb
        {
            Text = Loc.GetString("container-vehicle-verb-enter"),
            Act = () => StartEntryInteraction(ent, entering)
        };
        args.Verbs.Add(enterVerb);
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

    private void StartEntryInteraction(Entity<ContainerVehicleEntryComponent> vehicle, EntityUid entering)
    {
        if (!CanEnterViaInteraction(vehicle.Owner, entering))
            return;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            entering,
            vehicle.Comp.EntryDelay,
            new ContainerVehicleEntryEvent(),
            vehicle.Owner,
            target: vehicle.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
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
