using Content.Shared.Body.Surgery;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.Body.Surgery.Systems;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, DoAfterEvent<UseToolDoAfter>>(OnToolDoAfter);

        SubscribeLocalEvent<OperationComponent, AfterInteractUsingEvent>(OnOperationAfterInteractUsing);

        SubscribeLocalEvent<SurgeryDrapesComponent, OperationSelectedMessage>(OnOperationSelected);
        SubscribeLocalEvent<OperationComponent, OrganSelectedMessage>(OnOrganSelected);
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

    private void OnOperationAfterInteractUsing(EntityUid uid, OperationComponent comp, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // insert organs into part, part onto body, implants into cavity
        var user = args.User;
        args.Handled = Operation.TryInsertItem(comp, args.Used);
        if (args.Handled)
        {
            var userName = Identity.Name(user, EntityManager);
            Popup.PopupEntity(Loc.GetString("surgery-insert-success", ("user", userName), ("part", comp.Part), ("item", args.Used)), user, user);
        }
    }

    private void OnOperationSelected(EntityUid uid, SurgeryDrapesComponent comp, OperationSelectedMessage msg)
    {
        // TODO SURGERY: validate all input here
        if (Operation.StartOperation(msg.Target, msg.Part, uid, msg.Operation, out var operation))
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
        Popup.PopupEntity(msg, target);
    }

    private void OnOrganSelected(EntityUid uid, OperationComponent comp, OrganSelectedMessage msg)
    {
        // TODO SURGERY: validate organ
        Operation.SelectOrgan(comp, msg.Organ);
    }

    protected override bool OpenSelectUi(EntityUid uid, EntityUid surgeon, EntityUid target, Enum key, BoundUserInterfaceState state)
    {
        if (!TryComp<ActorComponent>(surgeon, out var actor) ||
            !_ui.TryOpen(uid, key, actor.PlayerSession))
            return false;

        return _ui.TrySetUiState(uid, key, state, actor.PlayerSession);
    }
}
