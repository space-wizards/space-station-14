using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.Body.Surgery.Systems;

public abstract class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly OperationSystem Operation = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryDrapesComponent, AfterInteractEvent>(OnDrapesAfterInteract);

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
        if (args.Target == null || !_timing.IsFirstTimePredicted)
            return;

        // cancelling an operation
        var target = args.Target.Value;
        var user = args.User;
        if (TryComp<OperationComponent>(target, out var operation))
        {
            if (operation.Tags.Count == 0)
            {
                if (_net.IsClient)
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
        Popup.PopupEntity(msg, target);
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
        if (!TryComp<BodyPartComponent>(part, out var bodyPart) || !TryComp<OperationComponent>(target, out var operation))
            return false;

        var drapes = operation.Drapes;

        var organs = _body.GetPartOrgans(part, bodyPart)
            .Select((child, _) => child.Item1);
        var state = new SelectOrganUiState(target, organs.ToArray());
        return OpenSelectUi(drapes, surgeon, target, SelectOrganUiKey.Key, state);
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
