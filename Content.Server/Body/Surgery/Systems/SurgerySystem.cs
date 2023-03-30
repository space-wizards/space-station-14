using Content.Shared.Body.Surgery;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.Body.Surgery.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.Body.Surgery.Systems;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryDrapesComponent, OperationSelectedMessage>(OnOperationSelected);
        SubscribeLocalEvent<SurgeryDrapesComponent, OrganSelectedMessage>(OnOrganSelected);

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<SurgeryToolComponent, DoAfterEvent<UseToolDoAfter>>(OnToolDoAfter);

        SubscribeLocalEvent<OperationComponent, AfterInteractUsingEvent>(OnOperationAfterInteractUsing);
    }

    private void OnOperationSelected(EntityUid uid, SurgeryDrapesComponent comp, OperationSelectedMessage msg)
    {
        // TODO SURGERY: validate all input here
        if (Operation.StartOperation(msg.Target, msg.Part, uid, msg.Operation, out var operation))
        {
            Logger.InfoS("surgery", $"Player {ToPrettyString(msg.User):player} started operation {operation.Prototype!.Name} on {ToPrettyString(msg.Target):target} (part {msg.Part})");
            DoDrapesStartPopups(msg.User, msg.Part, operation);
        }
    }

    private void DoDrapesStartPopups(EntityUid user, EntityUid target, OperationComponent operation)
    {
        var id = PopupId("prepare-start", user, target, operation.Part);
        var name = operation.Prototype!.Name;
        var userName = Identity.Name(user, EntityManager);
        var targetName = Identity.Name(target, EntityManager);
        var msg = Loc.GetString(id, ("user", userName), ("target", targetName), ("part", operation.Part), ("operation", name));
        Popup.PopupEntity(msg, user);
    }

    private void OnOrganSelected(EntityUid uid, SurgeryDrapesComponent comp, OrganSelectedMessage msg)
    {
        // TODO SURGERY: validate organ (and target if its kept)
        if (TryComp<OperationComponent>(msg.Target, out var operation))
            Operation.SelectOrgan(operation, msg.Organ);
    }

    private void OnToolAfterInteract(EntityUid uid, SurgeryToolComponent comp, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp<OperationComponent>(args.Target, out var operation) || operation.Busy)
            return;

        var target = args.Target.Value;
        var user = args.User;
        if (!Operation.CanPerform(target, user, operation, comp, out var step))
        {
            // can always use cautery to stop an operation
            if (!HasComp<CauteryComponent>(uid))
            {
                Popup.PopupEntity(Loc.GetString("surgery-step-not-useful"), user, user);
                return;
            }
        }

        args.Handled = true;
        // TODO: remove when doafter work
//        if (comp.Delay <= 0)
        if (true)
        {
            HandleTool(uid, comp, user, target, operation);
            return;
        }

        if (step != null)
        {
            var context = new SurgeryStepContext(target, user, operation, comp, step.ID, Operation, this);
            step.OnPerformDelayBegin(context);
        }

        var doAfter = new DoAfterEventArgs(user, comp.Delay, target: target, used: uid)
        {
            RaiseOnUser = false,
            RaiseOnTarget = false,
            RaiseOnUsed = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true // copy pasted this from sticky, it might not be needed
        };
        // TODO: merge doafter refactor so doafterevent is predicted
        _doAfter.DoAfter(doAfter, new UseToolDoAfter());
        Operation.SetBusy(operation, true);
    }

    private void OnToolDoAfter(EntityUid uid, SurgeryToolComponent comp, DoAfterEvent<UseToolDoAfter> args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (!TryComp<OperationComponent>(target, out var operation))
            return;

        Operation.SetBusy(operation, false);
        if (!args.Handled && !args.Cancelled)
        {
            args.Handled = true;
            HandleTool(uid, comp, args.Args.User, target, operation);
        }
    }

    private void HandleTool(EntityUid uid, SurgeryToolComponent comp, EntityUid user, EntityUid target, OperationComponent operation)
    {
        if (!Operation.TryPerform(target, user, operation, comp))
        {
            // if using a cautery, immediately stop the surgery
            if (HasComp<CauteryComponent>(uid))
            {
                var userName = Identity.Name(user, EntityManager);
                var targetName = Identity.Name(target, EntityManager);
                Popup.PopupEntity(Loc.GetString("surgery-aborted", ("user", userName), ("target", targetName)), user, user);
                RemComp<OperationComponent>(target);
            }
            else
            {
                Popup.PopupEntity(Loc.GetString("surgery-step-not-useful"), user, user);
            }
        }
    }

    private void OnOperationAfterInteractUsing(EntityUid uid, OperationComponent comp, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // insert organs into part, part onto body, implants into cavity
        var user = args.User;
        args.Handled = Operation.TryInsertItem(comp, args.Used);
        if (args.Handled)
        {
            var id = PopupId("insert-success", user, uid, comp.Part);
            var userName = Identity.Name(user, EntityManager);
            var targetName = Identity.Name(uid, EntityManager);
            var msg = Loc.GetString(id, ("user", userName), ("target", targetName), ("part", comp.Part), ("item", args.Used));
            Popup.PopupEntity(msg, user);
        }
    }

    protected override bool OpenSelectUi(EntityUid uid, EntityUid surgeon, EntityUid target, Enum key, BoundUserInterfaceState state)
    {
        if (!TryComp<ActorComponent>(surgeon, out var actor) ||
            !_ui.TryOpen(uid, key, actor.PlayerSession))
            return false;

        return _ui.TrySetUiState(uid, key, state, actor.PlayerSession);
    }

    private string PopupId(string type, EntityUid user, EntityUid target, EntityUid part)
    {
        var self = (user == target)
            ? "-self"
            : "";
        var zone = (part == target)
            ? "-no-zone"
            : "";

        return $"surgery-{type}{self}{zone}-popup";
    }

}
