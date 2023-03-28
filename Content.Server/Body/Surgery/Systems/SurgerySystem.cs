using Content.Server.Body.Surgery.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Systems;
using Content.Shared.Body.Surgery.UI;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.Body.Surgery.Systems;

// TODO: move 90% of this shit into shared
public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly OperationSystem _operation = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OperationComponent, OrganSelectedMessage>(OnOrganSelected);

        SubscribeLocalEvent<SurgeryDrapesComponent, AfterInteractEvent>(OnDrapesAfterInteract);
        SubscribeLocalEvent<SurgeryDrapesComponent, OperationSelectedMessage>(OnOperationSelected);

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<SurgeryToolComponent, DoAfterEvent<SurgeryToolData>>(OnToolDoAfter);
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
        // TODO SURGERY: support operating on individual surgery targets
        if (!TryComp<ActorComponent>(user, out var actor) || !TryComp<BodyComponent>(target, out var body))
            return;

        _ui.TryOpen(uid, SurgeryUiKey.Key, actor.PlayerSession);
        // TODO SURGERY: see above
        var state = GetDrapesUIState(target, user, body);
        _ui.TrySetUiState(uid, SurgeryUiKey.Key, state, actor.PlayerSession);

        args.Handled = true;
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

    private SurgeryUIState GetDrapesUIState(EntityUid target, EntityUid user, BodyComponent body)
    {
        var parts = _body.GetBodyChildren(target, body)
            .Select((child, _) => child.Item1);
        return new SurgeryUIState(target, user, parts.ToArray());
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
                _popup.PopupEntity(Loc.GetString("surgery-aborted", ("user", user), ("target", target)), user);
                RemComp<OperationComponent>(target);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("surgery-step-not-useful"), user, user);
            }
        }
    }

    public override bool SelectOrgan(EntityUid surgeon, EntityUid target)
    {
        if (!TryComp<ActorComponent>(surgeon, out var actor) || !TryComp<BodyComponent>(target, out var body))
            return false;

        // TODO: make server body component, or add our own one
//        body.OrganSelectionUI?.Open(actor.PlayerSession);
        return true;
    }
}
