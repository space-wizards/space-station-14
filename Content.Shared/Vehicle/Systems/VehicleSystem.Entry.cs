using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
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
            !args.CanInteract)
            return;

        var user = args.User;
        if (CanEnterViaInteraction(ent.Owner, user))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("container-vehicle-verb-enter"),
                Act = () => StartEntryInteraction(ent, user)
            };
            args.Verbs.Add(enterVerb);
        }
        else if (CanExitViaInteraction(ent.Owner, user))
        {
            var exitVerb = new AlternativeVerb
            {
                Text = Loc.GetString("container-vehicle-verb-remove-operator"),
                Priority = 1,
                Act = () => StartExitInteraction(ent, user)
            };
            args.Verbs.Add(exitVerb);
        }
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
            _popup.PopupEntity(Loc.GetString(component.EntryDeniedPopup, ("vehicle", uid)),
                Identity.Entity(args.User, EntityManager));
            return;
        }

        if (!CanEnterViaInteraction(uid, args.User) ||
            !TryEnter((uid, containerVehicle), args.User))
            return;

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnExitCompleted(EntityUid uid, ContainerVehicleEntryComponent component, ContainerVehicleExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryExit(uid))
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

    private void StartExitInteraction(Entity<ContainerVehicleEntryComponent> vehicle, EntityUid user)
    {
        if (!CanExitViaInteraction(vehicle.Owner, user) ||
            !TryGetOperator(vehicle.Owner, out var operatorEnt))
            return;

        if (user == vehicle.Owner || user == operatorEnt.Value.Owner)
        {
            TryExit(vehicle.Owner);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user,
            vehicle.Comp.ExitDelay,
            new ContainerVehicleExitEvent(),
            vehicle.Owner,
            target: vehicle.Owner)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        _popup.PopupEntity(Loc.GetString(vehicle.Comp.OperatorRemovalPopup,
            ("vehicle", vehicle.Owner), ("user", Identity.Entity(user, EntityManager))),
            vehicle.Owner, PopupType.Large);
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

    private bool CanExitViaInteraction(EntityUid vehicle, EntityUid user)
    {
        if (!HasOperator(vehicle))
            return false;

        var attempt = new ContainerVehicleExitAttemptEvent(user);
        RaiseLocalEvent(vehicle, attempt);
        return !attempt.Cancelled;
    }
}
