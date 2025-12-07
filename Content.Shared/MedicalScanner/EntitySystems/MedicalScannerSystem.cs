using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Cloning;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DragDrop;
using Content.Shared.MedicalScanner.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.MedicalScanner.EntitySystems;

/// <summary>
/// System for handling medical scanner logic, interactions, and connections.
/// </summary>
public sealed class MedicalScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly CloningConsoleSystem _cloningConsole = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    private const float UpdateRate = 1f;
    private float _updateDif;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedicalScannerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MedicalScannerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
        SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<MedicalScannerComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<MedicalScannerComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<MedicalScannerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<MedicalScannerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MedicalScannerComponent, CanDropTargetEvent>(OnCanDragDropOn);
    }

    private void OnCanDragDropOn(Entity<MedicalScannerComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= CanScannerInsert(ent.AsNullable(), args.Dragged);
    }

    /// <summary>
    /// Checks if a given entity can be inserted into the scanner.
    /// </summary>
    public bool CanScannerInsert(Entity<MedicalScannerComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return HasComp<BodyComponent>(target);
    }

    private void OnComponentInit(Entity<MedicalScannerComponent> ent, ref ComponentInit args)
    {
        base.Initialize();
        ent.Comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent.Owner, $"scanner-bodyContainer");
        _deviceLink.EnsureSinkPorts(ent.Owner, MedicalScannerComponent.ScannerPort);
    }

    private void OnRelayMovement(Entity<MedicalScannerComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        if (!_blocker.CanInteract(args.Entity, ent.Owner))
            return;

        EjectBody(ent.AsNullable());
    }

    private void AddInsertOtherVerb(Entity<MedicalScannerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using == null ||
            !args.CanAccess ||
            !args.CanInteract ||
            IsOccupied(ent.Comp) ||
            !CanScannerInsert(ent.AsNullable(), args.Using.Value))
            return;

        var name = Loc.GetString("generic-unknown-title");
        if (TryComp(args.Using.Value, out MetaDataComponent? metadata))
            name = metadata.EntityName;

        var target = args.Target;
        InteractionVerb verb = new()
        {
            Act = () => InsertBody(ent.AsNullable(), target),
            Category = VerbCategory.Insert,
            Text = name
        };
        args.Verbs.Add(verb);
    }

    private void AddAlternativeVerbs(Entity<MedicalScannerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb.
        if (IsOccupied(ent.Comp))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(ent.AsNullable()),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant"),
                Priority = 1 // Promote to top to make ejecting the ALT-click action.
            };
            args.Verbs.Add(verb);
        }

        // Self-insert verb.
        var user = args.User;
        if (!IsOccupied(ent.Comp) &&
            CanScannerInsert(ent.AsNullable(), args.User) &&
            _blocker.CanMove(args.User))
        {
            AlternativeVerb verb = new()
            {
                Act = () => InsertBody(ent.AsNullable(), user),
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnDestroyed(Entity<MedicalScannerComponent> ent, ref DestructionEventArgs args)
    {
        EjectBody(ent.AsNullable());
    }

    private void OnDragDropOn(Entity<MedicalScannerComponent> ent, ref DragDropTargetEvent args)
    {
        InsertBody(ent.AsNullable(), args.Dragged);
    }

    private void OnPortDisconnected(Entity<MedicalScannerComponent> ent, ref PortDisconnectedEvent args)
    {
        ent.Comp.ConnectedConsole = null;
        Dirty(ent);
    }

    private void OnAnchorChanged(Entity<MedicalScannerComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
            return;

        if (args.Anchored)
        {
            _cloningConsole.RecheckConnections(ent.Comp.ConnectedConsole.Value, console.CloningPod, ent.Owner);
            return;
        }

        _cloningConsole.UpdateUserInterface((ent.Comp.ConnectedConsole.Value, console));
        Dirty(ent);
    }

    private MedicalScannerStatus GetStatus(Entity<MedicalScannerComponent> ent)
    {
        if (_powerReceiver.IsPowered(ent.Owner))
        {
            var body = ent.Comp.BodyContainer.ContainedEntity;
            if (body == null)
                return MedicalScannerStatus.Open;

            // Is not alive or dead or critical.
            if (!TryComp<MobStateComponent>(body.Value, out var state))
                return MedicalScannerStatus.Yellow;

            return GetStatusFromDamageState((body.Value, state));
        }
        return MedicalScannerStatus.Off;
    }

    public static bool IsOccupied(MedicalScannerComponent scannerComponent)
    {
        return scannerComponent.BodyContainer.ContainedEntity != null;
    }

    private MedicalScannerStatus GetStatusFromDamageState(Entity<MobStateComponent> ent)
    {
        if (_mobState.IsAlive(ent))
            return MedicalScannerStatus.Green;

        if (_mobState.IsCritical(ent))
            return MedicalScannerStatus.Red;

        if (_mobState.IsDead(ent))
            return MedicalScannerStatus.Death;

        return MedicalScannerStatus.Yellow;
    }

    private void UpdateAppearance(Entity<MedicalScannerComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            _appearance.SetData(ent.Owner, MedicalScannerVisuals.Status, GetStatus(ent), appearance);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateDif += frameTime;
        if (_updateDif < UpdateRate)
            return;

        _updateDif -= UpdateRate;

        var query = EntityQueryEnumerator<MedicalScannerComponent>();
        while (query.MoveNext(out var uid, out var scanner))
        {
            UpdateAppearance((uid, scanner));
        }
    }

    public void InsertBody(Entity<MedicalScannerComponent?> ent, EntityUid toInsert)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.BodyContainer.ContainedEntity != null)
            return;

        if (!HasComp<BodyComponent>(toInsert))
            return;

        _container.Insert(toInsert, ent.Comp.BodyContainer);
        UpdateAppearance((ent.Owner, ent.Comp));
    }

    public void EjectBody(Entity<MedicalScannerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;

        _container.Remove(contained, ent.Comp.BodyContainer);
        _climb.ForciblySetClimbing(contained, ent.Owner);
        UpdateAppearance((ent.Owner, ent.Comp));
    }
}
