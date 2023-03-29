using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Body.Surgery.Systems;

public abstract class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly OperationSystem _operation = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryDrapesComponent, AfterInteractEvent>(OnDrapesAfterInteract);

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<SurgeryToolComponent, DoAfterEvent<SurgeryToolData>>(OnToolDoAfter);

        SubscribeLocalEvent<OperationComponent, AfterInteractUsingEvent>(OnOperationAfterInteractUsing);

        SubscribeLocalEvent<SurgeryDrapesComponent, OperationSelectedMessage>(OnOperationSelected);
        SubscribeLocalEvent<BodyPartComponent, OrganSelectedMessage>(OnOrganSelected);

        foreach (var operation in _proto.EnumeratePrototypes<SurgeryOperationPrototype>())
        {
            foreach (var step in operation.Steps)
            {
                if (!_proto.HasIndex<SurgeryStepPrototype>(step.ID))
                {
                    Logger.WarningS("surgery",
                        $"Invalid {nameof(SurgeryStepPrototype)} found in surgery operation with id {operation.ID}: No step found with id {step.ID}");
                }
            }
        }
    }

    private void OnDrapesAfterInteract(EntityUid uid, SurgeryDrapesComponent comp, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        // cancelling an operation
        var target = args.Target.Value;
        var user = args.User;
        if (TryComp<OperationComponent>(target, out var operation))
        {
            if (operation.Tags.Count == 0)
            {
                DoDrapesCancelPopups(uid, user, target);
                RemComp<OperationComponent>(target);
            }

            args.Handled = true;
            return;
        }

        // starting a new operation
        args.Handled = SelectOperation(uid, user, target);
    }

    private void DoDrapesCancelPopups(EntityUid drapes, EntityUid user, EntityUid target)
    {
        var part = Comp<OperationComponent>(target).Part;
        var self = (user == target)
            ? "-self"
            : "";
        var zone = (part == target)
            ? "-no-zone"
            : "";
        var id = $"surgery-prepare-cancel{self}{zone}-popup";
        var msg = Loc.GetString(id, ("user", user), ("drapes", drapes), ("target", target), ("part", part));
        _popup.PopupEntity(msg, target);
    }

    private void OnToolAfterInteract(EntityUid uid, SurgeryToolComponent comp, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp<OperationComponent>(args.Target, out var operation) || operation.Busy)
            return;

        var target = args.Target.Value;
        var user = args.User;
        if (!_operation.CanPerform(target, user, operation, comp, out var step))
        {
            // can always use cautery to stop an operation
            if (!HasComp<CauteryComponent>(uid))
            {
                _popup.PopupEntity(Loc.GetString("surgery-step-not-useful"), user, user);
                return;
            }
        }

        args.Handled = true;
        if (comp.Delay <= 0)
        {
            HandleTool(uid, comp, user, target, operation);
            return;
        }

        if (step != null)
            _operation.DoBeginPopups(user, target, operation.Part, step.ID);

        var doAfter = new DoAfterEventArgs(user, comp.Delay, target: target, used: uid)
        {
            RaiseOnUser = false,
            RaiseOnTarget = false,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true // copy pasted this from sticky, it might not be needed
        };
        _doAfter.DoAfter(doAfter, new SurgeryToolData());
        _operation.SetBusy(operation, true);
    }

    private void OnToolDoAfter(EntityUid uid, SurgeryToolComponent comp, DoAfterEvent<SurgeryToolData> args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (!TryComp<OperationComponent>(target, out var operation))
            return;

        _operation.SetBusy(operation, false);
        if (!args.Handled && !args.Cancelled)
        {
            args.Handled = true;
            HandleTool(uid, comp, args.Args.User, target, operation);
        }
    }

    private void HandleTool(EntityUid uid, SurgeryToolComponent comp, EntityUid user, EntityUid target, OperationComponent operation)
    {
        if (!_operation.TryPerform(target, user, operation, comp))
        {
            // if using a cautery, immediately stop the surgery
            if (HasComp<CauteryComponent>(uid))
            {
                _popup.PopupEntity(Loc.GetString("surgery-aborted", ("user", user), ("target", target)), user, user);
                RemComp<OperationComponent>(target);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("surgery-step-not-useful"), user, user);
            }
        }
    }

    private void OnAfterInteractUsing(EntityUid uid, OperationComponent comp, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !_timing.IsFirstTimePredicted)

        // insert organs into part, part onto body, implants into cavity
        args.Handled = _operation.TryInsertItem(uid, args.User, comp, args.Used);
    }

    private void OnOperationSelected(EntityUid uid, SurgeryDrapesComponent comp, OperationSelectedMessage msg)
    {
        // TODO SURGERY: validate all input here
        if (_operation.StartOperation(msg.Target, msg.Part, msg.Operation, out var operation))
        {
            Logger.InfoS("surgery", $"Player {ToPrettyString(msg.User):player} started surgery {operation.Prototype!.Name} on {ToPrettyString(msg.Target)} (part {msg.Part})");
            DoDrapesStartPopups(msg.User, msg.Part, operation);
        }
    }

    private void DoDrapesStartPopups(EntityUid user, EntityUid target, OperationComponent operation)
    {
        var part = operation.Part;
        var name = operation.Prototype!.Name;
        var self = (user == target)
            ? "-self"
            : "";
        var zone = (part == target)
            ? "-no-zone"
            : "";

        var id = $"surgery-prepare-start{self}{zone}-popup";
        var msg = Loc.GetString(id, ("user", user), ("target", target), ("part", part), ("operation", name));
        _popup.PopupEntity(msg, target);
    }

    private void OnOrganSelected(EntityUid uid, OperationComponent comp, OrganSelectedMessage msg)
    {
        // TODO SURGERY: validate organ
        _operation.SelectOrgan(comp, msg.Organ);
    }

    /// <summary>
    /// Open the window for selecting an operation to do.
    /// </summary>
    public bool SelectOperation(EntityUid drapes, EntityUid surgeon, EntityUid target)
    {
        if (!TryComp<BodyComponent>(target, out var body))
            return false;

        // TODO SURGERY: support operating on individual bodyparts
        var parts = _body.GetBodyChildren(target, body)
            .Select((child, _) => child.Item1);
        var state = new SelectOperationUiState(target, surgeon, parts.ToArray());
        return OpenSelectUi(drapes, surgeon, target, SelectOperationUiKey.Key, state);
    }

    /// <summary>
    /// Open the window for selecting an organ to remove.
    /// </summary>
    public bool SelectOrgan(EntityUid part, EntityUid surgeon, EntityUid target)
    {
        if (!TryComp<BodyPartComponent>(part, out var bodyPart))
            return false;

        var organs = _body.GetPartOrgans(part, bodyPart)
            .Select((child, _) => child.Item1);
        var state = new SelectOrganUiState(target, surgeon, organs.ToArray());
        return OpenSelectUi(part, surgeon, target, SelectOrganUiKey.Key, state);
    }

    protected virtual bool OpenSelectUi(EntityUid uid, EntityUid surgeon, EntityUid target, Enum key, BoundUserInterfaceState state)
    {
        // client always predicts that opening the ui works
        return true;
    }

    /// <summary>
    /// Removes an organ then puts it in the surgeons hands if possible.
    /// </summary>
    /// <returns>true if the organ was removed successfully</returns>
    public bool RemoveOrgan(EntityUid surgeon, EntityUid organ)
    {
        if (!_body.DropOrgan(organ))
            return false;

        _hands.TryPickupAnyHand(surgeon, organ);
        return true;
    }
}
