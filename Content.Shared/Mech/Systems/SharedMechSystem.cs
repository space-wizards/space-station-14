using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Components;
using Content.Shared.Alert;
using Content.Shared.Body.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Materials;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Repairable.Events;
using Content.Shared.Storage.Components;
using Content.Shared.Throwing;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Mech.Systems;

public abstract partial class SharedMechSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly EntityWhitelistSystem _entWhitelist = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MechLockSystem _mechLock = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] protected readonly VehicleSystem Vehicle = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        InitializeRelay();

        SubscribeLocalEvent<MechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);
        SubscribeLocalEvent<MechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<MechComponent, EntityStorageIntoContainerAttemptEvent>(OnEntityStorageDump);
        SubscribeLocalEvent<MechComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<MechComponent, CanDropTargetEvent>(OnCanDragDrop);
        SubscribeLocalEvent<MechComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<MechComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MechComponent, RepairAttemptEvent>(OnRepairAttempt);

        SubscribeLocalEvent<MechEquipmentComponent, ShotAttemptedEvent>(OnMechEquipmentShotAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, AttemptMeleeEvent>(OnMechEquipmentMeleeAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, GettingUsedAttemptEvent>(OnMechEquipmentGettingUsedAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, ActivatableUIOpenAttemptEvent>(OnMechEquipmentUiOpenAttempt);
        SubscribeLocalEvent<MechComponent, UseHeldBypassAttemptEvent>(OnUseHeldBypass);

        SubscribeLocalEvent<MechPilotComponent, CanAttackFromContainerEvent>(OnCanAttackFromContainer);
        SubscribeLocalEvent<MechPilotComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<MechPilotComponent, GetActiveWeaponEvent>(OnGetActiveWeapon);
        SubscribeLocalEvent<MechPilotComponent, GetUsedEntityEvent>(OnPilotGetUsedEntity);
        SubscribeLocalEvent<MechPilotComponent, AccessibleOverrideEvent>(OnPilotAccessible);
        SubscribeLocalEvent<MechPilotComponent, GetShootingEntityEvent>(OnGetShootingEntity);

        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<MechComponent, MechExitEvent>(OnMechExit);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);

        SubscribeLocalEvent<MechComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<MechComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<MechComponent, RemoveBatteryEvent>(OnRemoveBattery);
        SubscribeLocalEvent<MechComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<MechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);
        SubscribeLocalEvent<MechComponent, PowerCellChangedEvent>(OnBatteryChanged);

        SubscribeLocalEvent<MechComponent, MechBrokenSoundEvent>(OnMechBrokenSound);
        SubscribeLocalEvent<MechComponent, MechEntrySuccessSoundEvent>(OnMechEntrySuccessSound);

        SubscribeAllEvent<RequestMechEquipmentSelectEvent>(OnEquipmentSelectRequest);
        SubscribeLocalEvent<MechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<MechComponent, EmpAttemptEvent>(OnEmpAttempt);
        SubscribeLocalEvent<MechComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnMapInit(Entity<MechComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent.Owner);

        foreach (var equipment in ent.Comp.StartingEquipment)
        {
            var equipmentEnt = EntityManager.PredictedSpawnAttachedTo(equipment, xform.Coordinates);
            InsertEquipment(ent.AsNullable(), equipmentEnt);
        }

        foreach (var module in ent.Comp.StartingModules)
        {
            var equipmentEnt = EntityManager.PredictedSpawnAttachedTo(module, xform.Coordinates);
            InsertEquipment(ent.AsNullable(), equipmentEnt);
        }

        ent.Comp.Integrity = ent.Comp.MaxIntegrity;
        ent.Comp.Airtight = false;

        SetIntegrity(ent.AsNullable(), ent.Comp.MaxIntegrity);
        _actionBlocker.UpdateCanMove(ent.Owner);

        Dirty(ent);
        UpdateMechUi(ent.Owner);
        UpdateHealthAlert(ent.AsNullable());
    }

    private void OnEjectPilotEvent(Entity<MechComponent> ent, ref MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.Performer,
            ent.Comp.EntryDelay,
            new MechExitEvent(),
            ent.Owner,
            target: ent.Owner)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnStartup(Entity<MechComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.PilotSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.PilotSlotId);
        ent.Comp.EquipmentContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.EquipmentContainerId);
        ent.Comp.BatterySlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.BatterySlotId);
        ent.Comp.ModuleContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModuleContainerId);
    }

    private void OnDestruction(Entity<MechComponent> ent, ref DestructionEventArgs args)
    {
        TryEject(ent.AsNullable());
        UpdateAppearance(ent);
    }

    private void OnEntityStorageDump(Entity<MechComponent> ent, ref EntityStorageIntoContainerAttemptEvent args)
    {
        // There's no reason we should dump into /any/ of the mech's containers.
        args.Cancelled = true;
    }

    private void ManageVirtualItems(Entity<MechComponent> mechEnt, EntityUid pilot, bool create)
    {
        if (!HasComp<HandsComponent>(pilot))
            return;

        if (create)
        {
            // Creating virtual items to block hands.
            var blocking = mechEnt.Comp.CurrentSelectedEquipment ?? mechEnt.Owner;

            foreach (var hand in _hands.EnumerateHands(pilot))
            {
                if (_hands.TryGetHeldItem(pilot, hand, out _))
                    continue;

                if (_virtualItem.TrySpawnVirtualItemInHand(blocking, pilot, out var virtualItem, dropOthers: false))
                    EnsureComp<UnremoveableComponent>(virtualItem.Value);
            }
        }
        else
        {
            // Remove virtual items for mech and equipment.
            _virtualItem.DeleteInHandsMatching(pilot, mechEnt.Owner);
            foreach (var eq in mechEnt.Comp.EquipmentContainer.ContainedEntities)
            {
                _virtualItem.DeleteInHandsMatching(pilot, eq);
            }
        }
    }

    /// <summary>
    /// Throws an entity away from the mech in a random direction at a random speed.
    /// </summary>
    private void ScatterEntityFromMech(EntityUid uid, float minSpeed = 4f, float maxSpeed = 7f)
    {
        var direction = _random.NextAngle().ToWorldVec();
        var speed = _random.NextFloat(minSpeed, maxSpeed);
        _throwing.TryThrow(uid, direction, speed);
    }

    private void UpdateAppearance(Entity<MechComponent> ent, AppearanceComponent? appearance = null)
    {
        var isOpen = !Vehicle.HasOperator(ent.Owner);

        _appearance.SetData(ent.Owner, MechVisuals.Open, isOpen, appearance);
        _appearance.SetData(ent.Owner, MechVisuals.Broken, ent.Comp.Broken, appearance);
    }

    private void OnDragDrop(Entity<MechComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.EntryDelay,
            new MechEntryEvent(),
            ent.Owner,
            target: ent.Owner,
            used: args.Dragged)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnCanDragDrop(Entity<MechComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;

        args.CanDrop |= CanInsert(ent.AsNullable(), args.Dragged);
    }

    private void OnOperatorSet(Entity<MechComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.NewOperator is { } newOperator)
            SetupUser(ent, newOperator);

        if (args.OldOperator is { } oldOperator)
            RemoveUser(ent, oldOperator);

        UpdateAppearance(ent);

        // Toggle movement energy drain based on pilot presence.
        var drainToggle = new MechMovementDrainToggleEvent(args.NewOperator != null);
        RaiseLocalEvent(ent, ref drainToggle);
    }

    private void SetupUser(Entity<MechComponent> mechEnt, EntityUid pilot)
    {
        var rider = EnsureComp<MechPilotComponent>(pilot);
        rider.Mech = mechEnt.Owner;
        Dirty(pilot, rider);

        // Drop held items upon entering.
        if (!TryComp<HandsComponent>(pilot, out var handsComp))
            return;

        foreach (var hand in _hands.EnumerateHands(pilot))
        {
            if (_hands.TryGetHeldItem(pilot, hand, out _))
                _hands.TryDrop((pilot, handsComp), hand);
        }

        // Play entry success sound.
        if (mechEnt.Comp.EntrySuccessSound != null)
        {
            var ev = new MechEntrySuccessSoundEvent(mechEnt.Owner, mechEnt.Comp.EntrySuccessSound);
            RaiseLocalEvent(mechEnt.Owner, ref ev);
        }

        ManageVirtualItems(mechEnt, pilot, create: true);
    }

    private void RemoveUser(Entity<MechComponent> mechEnt, EntityUid pilot)
    {
        RemComp<MechPilotComponent>(pilot);
        RemComp<ActionsDisplayRelayComponent>(pilot);
        RemComp<AlertsDisplayRelayComponent>(pilot);

        ManageVirtualItems(mechEnt, pilot, create: false);

        if (TryComp<ActionsComponent>(pilot, out var pilotActions))
            Dirty(pilot, pilotActions);
        if (TryComp<AlertsComponent>(pilot, out var pilotAlerts))
            Dirty(pilot, pilotAlerts);
    }

    private void OnGetVerb(Entity<MechComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;


        // Enter verb (when user can insert).
        var user = args.User;
        if (CanInsert(ent.AsNullable(), user))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager,
                        user,
                        ent.Comp.EntryDelay,
                        new MechEntryEvent(),
                        ent.Owner,
                        target: ent.Owner)
                    {
                        BreakOnMove = true,
                        NeedHand = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                },
            };
            args.Verbs.Add(enterVerb);
        }
        // Exit verb (when there's an operator).
        else if (Vehicle.HasOperator(ent.Owner))
        {
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager,
                        user,
                        ent.Comp.ExitDelay,
                        new MechExitEvent(),
                        ent.Owner,
                        target: ent.Owner)
                    {
                        BreakOnMove = true,
                    };

                    if (user != ent.Owner && user != ent.Comp.PilotSlot.ContainedEntity)
                    {
                        _popup.PopupPredicted(Loc.GetString("mech-eject-pilot-alert-popup",
                                ("item", ent.Owner),
                                ("user", Identity.Entity(user, EntityManager))),
                            ent.Owner,
                            user);
                    }

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                },
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnEmagged(Entity<MechComponent> ent, ref GotEmaggedEvent args)
    {
        if (!ent.Comp.BreakOnEmag)
            return;

        if (HasComp<EmaggedComponent>(ent.Owner))
            return;

        args.Handled = true;
        ent.Comp.EquipmentWhitelist = null;
        ent.Comp.ModuleWhitelist = null;
        Dirty(ent);
    }

    private void OnRepairAttempt(Entity<MechComponent> ent, ref RepairAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Broken)
            return;

        args.Cancelled = true;
    }

    private void OnMechEntry(Entity<MechComponent> ent, ref MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // Allow entry if locks are not active; block only if active and user lacks access.
        var performer = args.Args.User;
        var pilot = args.Args.Used ?? performer;

        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, performer))
            return;

        if (!Vehicle.CanOperate(ent.Owner, performer))
        {
            _popup.PopupClient(Loc.GetString("mech-no-enter-popup", ("item", ent.Owner)), performer, performer);
            return;
        }

        if (!TryInsert(ent.AsNullable(), pilot))
            return;

        args.Handled = true;

        UpdateMechUi(ent.Owner);
        UpdateBatteryAlert(ent.AsNullable());
        UpdateHealthAlert(ent.AsNullable());

        // Ensure pilot has required components.
        var pilotActions = EnsureComp<ActionsComponent>(pilot);
        var pilotAlerts = EnsureComp<AlertsComponent>(pilot);

        // Setup actions relay.
        var actionsRelay = EnsureComp<ActionsDisplayRelayComponent>(pilot);
        actionsRelay.Source = ent.Owner;
        actionsRelay.InteractAsSource = true;

        // Setup alerts relay.
        var alertsRelay = EnsureComp<AlertsDisplayRelayComponent>(pilot);
        alertsRelay.Source = ent.Owner;
        alertsRelay.InteractAsSource = true;

        // Notify client of changes.
        Dirty(pilot, pilotActions);
        Dirty(pilot, pilotAlerts);
        Dirty(pilot, actionsRelay);
        Dirty(pilot, alertsRelay);
    }

    private void OnMechExit(Entity<MechComponent> ent, ref MechExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var pilot = Vehicle.GetOperatorOrNull(ent.Owner);

        TryEject(ent.AsNullable());
        UpdateMechUi(ent.Owner);

        if (pilot.HasValue)
            _actionBlocker.UpdateCanMove(pilot.Value);

        args.Handled = true;
    }

    private void OnOpenUi(Entity<MechComponent> ent, ref MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(ent);
    }

    private void OnContainerChanged(Entity<MechComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container == ent.Comp.BatterySlot)
        {
            _actionBlocker.UpdateCanMove(ent.Owner);

            UpdateMechUi(ent.Owner);
            UpdateBatteryAlert(ent.AsNullable());
        }
        else if (args.Container == ent.Comp.PilotSlot)
        {
            // Pilot entered, update alerts.
            UpdateBatteryAlert(ent.AsNullable());
            UpdateHealthAlert(ent.AsNullable());

            // Lock battery slot while occupied.
            if (TryComp<ItemSlotsComponent>(ent.Owner, out var slots))
                _itemSlots.SetLock(ent.Owner, ent.Comp.BatterySlotId, true, slots);
        }
    }

    private void OnContainerChanged(Entity<MechComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container == ent.Comp.BatterySlot)
        {
            _actionBlocker.UpdateCanMove(ent.Owner);

            UpdateMechUi(ent.Owner);
        }
        else if (args.Container == ent.Comp.PilotSlot)
        {
            // Unlock battery slot when unoccupied.
            if (TryComp<ItemSlotsComponent>(ent.Owner, out var slots))
                _itemSlots.SetLock(ent.Owner, ent.Comp.BatterySlotId, false, slots);
        }
        else if (args.Container == ent.Comp.EquipmentContainer)
        {
            // Clear owner and raise removal event.
            if (!TryComp<MechEquipmentComponent>(args.Entity, out var eq))
                return;

            if (eq.EquipmentOwner == ent.Owner)
                eq.EquipmentOwner = null;
        }
    }

    private void OnRemoveBattery(Entity<MechComponent> ent, ref RemoveBatteryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        RemoveBattery(ent.AsNullable());
        _actionBlocker.UpdateCanMove(ent.Owner);

        args.Handled = true;

        UpdateMechUi(ent.Owner);
    }

    private void OnInteractUsing(Entity<MechComponent> ent, ref InteractUsingEvent args)
    {
        if (Vehicle.HasOperator(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("mech-cannot-modify-closed-popup"), args.User, args.User);
            return;
        }

        // Allow prying removal when a battery is present.
        if (_tool.HasQuality(args.Used, PryingQuality) && ent.Comp.BatterySlot.ContainedEntity != null)
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.BatteryRemovalDelay,
                new RemoveBatteryEvent(),
                ent.Owner,
                target: ent.Owner,
                used: args.Target)
            {
                BreakOnMove = true
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
            return;
        }

        // Try forwarding material sheets into generator module storage when hatch is open.
        if (HasComp<MaterialComponent>(args.Used))
        {
            foreach (var mod in ent.Comp.ModuleContainer.ContainedEntities)
            {
                if (!TryComp<MaterialStorageComponent>(mod, out var storage)
                    || !_material.TryInsertMaterialEntity(args.User, args.Used, mod, storage))
                    continue;

                args.Handled = true;
            }
        }
    }

    private void OnMechCanMoveEvent(Entity<MechComponent> ent, ref UpdateCanMoveEvent args)
    {
        // Block movement if mech is in broken state or has no energy/integrity.
        var hasCharge = _powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) &&
                        _battery.GetCharge(battery.Value.AsNullable()) > 0;
        if (ent.Comp.Broken || ent.Comp.Integrity <= 0 || !hasCharge)
        {
            args.Cancel();
            return;
        }

        // Block movement if mech is locked and pilot lacks access.
        var pilot = Vehicle.GetOperatorOrNull(ent.Owner);
        if (pilot.HasValue && !_mechLock.CheckAccessWithFeedback(ent.Owner, pilot.Value))
        {
            args.Cancel();
            return;
        }

        // Block movement if the pilot has no hands.
        if (pilot.HasValue && !HasComp<HandsComponent>(pilot.Value))
            args.Cancel();
    }

    private void OnBatteryChanged(Entity<MechComponent> ent, ref PowerCellChangedEvent args)
    {
        // Battery changed, update UI and alerts.
        UpdateMechUi(ent.Owner);
        UpdateBatteryAlert(ent.AsNullable());
    }

    private void OnMechBrokenSound(Entity<MechComponent> ent, ref MechBrokenSoundEvent args)
    {
        _audio.PlayPredicted(args.Sound, ent.Owner, ent.Owner);
    }

    private void OnMechEntrySuccessSound(Entity<MechComponent> ent, ref MechEntrySuccessSoundEvent args)
    {
        var pilot = Vehicle.GetOperatorOrNull(ent.Owner);
        if (!pilot.HasValue)
            return;

        if (_gameTiming.IsFirstTimePredicted && _net.IsClient)
            _audio.PlayEntity(args.Sound, pilot.Value, ent.Owner);
    }

    private void OnEquipmentSelectRequest(RequestMechEquipmentSelectEvent args, EntitySessionEventArgs session)
    {
        var user = session.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryComp<MechPilotComponent>(user.Value, out var pilot))
            return;

        var mech = pilot.Mech;
        if (!TryComp<MechComponent>(mech, out var mechComp))
            return;

        if (args.Equipment == null)
        {
            mechComp.CurrentSelectedEquipment = null;
            _popup.PopupClient(Loc.GetString("mech-select-none-popup"), mech, mech);
        }
        else
        {
            var equipment = GetEntity(args.Equipment);
            if (Exists(equipment) && mechComp.EquipmentContainer.ContainedEntities.Any(e => e == equipment))
            {
                mechComp.CurrentSelectedEquipment = equipment;
                _popup.PopupClient(Loc.GetString("mech-select-popup", ("item", equipment)), mech, mech);
            }
        }

        Dirty(mech, mechComp);
        RefreshPilotHandVirtualItems(mech);
    }

    private static void OnToolUseAttempt(Entity<MechPilotComponent> ent, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target == ent.Comp.Mech)
            args.Cancelled = true;
    }

    private static void OnEmpAttempt(Entity<MechComponent> ent, ref EmpAttemptEvent args)
    {
        // mech (battery) is immune to emp.
        args.Cancelled = true;
    }

    private void OnBeingGibbed(Entity<MechComponent> ent, ref BeingGibbedEvent args)
    {
        // Eject pilot if present.
        if (ent.Comp.PilotSlot.ContainedEntity != null)
            TryEject(ent.AsNullable());

        if (ent.Comp.PilotSlot.ContainedEntity != null)
            args.GibbedParts.Add(ent.Comp.PilotSlot.ContainedEntity.Value);

        // TODO: Parts should fall out
        PredictedDel(ent.Owner);
    }

    private void ToggleMechUi(Entity<MechComponent> ent, EntityUid? user = null)
    {
        user ??= Vehicle.GetOperatorOrNull(ent.Owner);
        if (user == null)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(ent.Owner, MechUiKey.Key, actor.PlayerSession);
    }

    #region Mech Equipment

    /// <summary>
    /// Returns whether the given mech equipment can be used from hands.
    /// </summary>
    private bool IsMechEquipmentUsableFromHands(Entity<MechEquipmentComponent> ent)
    {
        if (!ent.Comp.BlockUseOutsideMech)
            return true;

        if (ent.Comp.EquipmentOwner.HasValue)
            return true;

        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            HasComp<MechComponent>(container.Owner))
            return true;

        return false;
    }

    private void OnMechEquipmentShotAttempt(Entity<MechEquipmentComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!IsMechEquipmentUsableFromHands(ent))
            args.Cancel();
    }

    private void OnMechEquipmentMeleeAttempt(Entity<MechEquipmentComponent> ent, ref AttemptMeleeEvent args)
    {
        args.Cancelled = !IsMechEquipmentUsableFromHands(ent);
    }

    private void OnMechEquipmentGettingUsedAttempt(Entity<MechEquipmentComponent> ent, ref GettingUsedAttemptEvent args)
    {
        // To avoid incorrect empty-hand prediction leading to unintended target activation.
        if (_net.IsClient)
            return;

        if (!ent.Comp.BlockUseOutsideMech)
            return;

        // If the equipment is not inside a mech, block using in hands.
        if (ent.Comp.EquipmentOwner == null)
            args.Cancel();
    }

    private void OnMechEquipmentUiOpenAttempt(Entity<MechEquipmentComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        // If equipment is outside of a mech, prevent its activatable UI from opening and from adding verbs.
        if (ent.Comp.EquipmentOwner == null)
            args.Cancel();
    }

    private void OnUseHeldBypass(Entity<MechComponent> ent, ref UseHeldBypassAttemptEvent args)
    {
        // Allow only using mech equipment items on a mech (i.e., inserting).
        if (!_hands.TryGetActiveItem(args.User, out var held))
            return;

        if (HasComp<MechEquipmentComponent>(held.Value))
            args.Bypass = true;
    }

    #endregion

    #region Mech Pilot

    private void OnCanAttackFromContainer(Entity<MechPilotComponent> ent, ref CanAttackFromContainerEvent args)
    {
        args.CanAttack = true;
    }

    private void OnGetMeleeWeapon(Entity<MechPilotComponent> ent, ref GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HandsComponent>(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        // Use the currently selected equipment if available, otherwise the mech itself.
        var weapon = mech.CurrentSelectedEquipment ?? ent.Comp.Mech;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnGetActiveWeapon(Entity<MechPilotComponent> ent, ref GetActiveWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        // Use the currently selected equipment if available, otherwise the mech itself.
        var weapon = mech.CurrentSelectedEquipment ?? ent.Comp.Mech;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnPilotGetUsedEntity(Entity<MechPilotComponent> ent, ref GetUsedEntityEvent args)
    {
        // Map pilot interactions to the currently selected equipment on their mech.
        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        if (!Vehicle.HasOperator(ent.Comp.Mech))
            return;

        if (mech.CurrentSelectedEquipment != null)
            args.Used = mech.CurrentSelectedEquipment;
    }

    private void OnPilotAccessible(Entity<MechPilotComponent> ent, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;
        args.Accessible = _interaction.IsAccessible(ent.Comp.Mech, args.Target);
    }

    private void OnGetShootingEntity(Entity<MechPilotComponent> ent, ref GetShootingEntityEvent args)
    {
        if (args.Handled)
            return;

        // Use the mech entity for shooting coordinates and physics instead of the pilot.
        args.ShootingEntity = ent.Comp.Mech;
        args.Handled = true;
    }

    #endregion
}
