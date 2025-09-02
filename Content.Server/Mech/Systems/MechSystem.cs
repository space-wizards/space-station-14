using Content.Server.Construction.Components;
using Content.Server.Construction;
using Content.Server.Mech.Components;
using Content.Server.Mech.Events;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech;
using Content.Shared.Popups;
using System.Linq;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Body.Events;
using Robust.Shared.Audio.Systems;
using Content.Shared.Vehicle;
using Content.Shared.Alert;
using Content.Shared.PowerCell;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Materials;
using Content.Server.Materials;
using Content.Shared.Containers.ItemSlots;
using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class MechSystem : SharedMechSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly MechLockSystem _lockSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MechComponent, RepairMechEvent>(OnRepairMechEvent);
        SubscribeLocalEvent<MechComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<MechComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<MechComponent, RemoveBatteryEvent>(OnRemoveBattery);
        SubscribeLocalEvent<MechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<MechComponent, MechExitEvent>(OnMechExit);
        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<MechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);
        SubscribeLocalEvent<MechComponent, PowerCellChangedEvent>(OnBatteryChanged);
        SubscribeLocalEvent<MechComponent, PowerCellSlotEmptyEvent>(OnBatteryChanged);

        SubscribeLocalEvent<MechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeAllEvent<RequestMechEquipmentSelectEvent>(OnEquipmentSelectRequest);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<MechComponent, MechBrokenSoundEvent>(OnMechBrokenSound);
        SubscribeLocalEvent<MechComponent, MechEntrySuccessSoundEvent>(OnMechEntrySuccessSound);
    }

    private void OnRepairMechEvent(EntityUid uid, MechComponent component, RepairMechEvent args)
    {
        RepairMech(uid, component);

        // Restore prototype-declared disassembly graph after successful repair
        var cc = EnsureComp<ConstructionComponent>(uid);
        _construction.ChangeGraph(uid, null, "MechDisassemble", "start", performActions: false, cc);
    }

    private void OnMechEntrySuccessSound(EntityUid uid, MechComponent component, MechEntrySuccessSoundEvent args)
    {
        var pilot = Vehicle.GetOperatorOrNull(uid);
        if (!pilot.HasValue)
            return;

        _audio.PlayEntity(args.Sound, Filter.Entities(pilot.Value), uid, false);
    }

    private void OnMechCanMoveEvent(EntityUid uid, MechComponent component, UpdateCanMoveEvent args)
    {
        // Block movement if mech is in broken state or has no energy/integrity
        var hasCharge = _powerCell.TryGetBatteryFromSlot(uid, out var battery) && battery.CurrentCharge > 0;
        if (component.Broken || component.Integrity <= 0 || !hasCharge)
        {
            args.Cancel();
            return;
        }

        // Block movement if mech is locked and pilot lacks access
        if (TryComp<MechLockComponent>(uid, out var lockComp) && lockComp.IsLocked)
        {
            var pilot = Vehicle.GetOperatorOrNull(uid);
            if (pilot.HasValue && !_lockSystem.CheckAccess(uid, pilot.Value, lockComp))
            {
                args.Cancel();
                return;
            }
        }

        // Block movement if the pilot has no hands
        if (Vehicle.TryGetOperator(uid, out var operatorEnt))
        {
            if (!HasComp<HandsComponent>(operatorEnt))
            {
                args.Cancel();
                return;
            }
        }
    }

    private void OnInteractUsing(EntityUid uid, MechComponent component, InteractUsingEvent args)
    {
        // Allow prying removal when a battery is present
        if (_toolSystem.HasQuality(args.Used, PryingQuality) && component.BatterySlot.ContainedEntity != null)
        {
            if (Vehicle.HasOperator(uid))
            {
                _popup.PopupEntity(Loc.GetString("mech-cannot-modify-closed-popup"), args.User);
                return;
            }

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.BatteryRemovalDelay,
                new RemoveBatteryEvent(), uid, target: uid, used: args.Target)
            {
                BreakOnMove = true
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
            return;
        }

        // Try forwarding material sheets into generator module storage when hatch is open
        if (!Vehicle.HasOperator(uid) && TryComp<MaterialComponent>(args.Used, out var materialComp))
        {
            foreach (var mod in component.ModuleContainer.ContainedEntities)
            {
                if (TryComp<MaterialStorageComponent>(mod, out var storage))
                {
                    if (_material.TryInsertMaterialEntity(args.User, args.Used, mod, storage))
                    {
                        args.Handled = true;
                        return;
                    }
                }
            }
        }
    }

    private void OnContainerChanged(EntityUid uid, MechComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container == component.BatterySlot)
        {
            Dirty(uid, component);
            _actionBlocker.UpdateCanMove(uid);

            UpdateUserInterface(uid, component);
            UpdateBatteryAlert((uid, component));
        }
        else if (args.Container == component.PilotSlot)
        {
            // Pilot entered, update alerts
            UpdateBatteryAlert((uid, component));
            UpdateHealthAlert((uid, component));

            // Lock battery slot while occupied
            if (TryComp<ItemSlotsComponent>(uid, out var slots))
                _itemSlots.SetLock(uid, component.BatterySlotId, true, slots);
        }
    }

    private void OnContainerChanged(EntityUid uid, MechComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container == component.BatterySlot)
        {
            Dirty(uid, component);
            _actionBlocker.UpdateCanMove(uid);

            UpdateUserInterface(uid, component);
        }
        else if (args.Container == component.PilotSlot)
        {
            // Pilot left, clear alerts
            var pilot = args.Entity;
            _alerts.ClearAlert(pilot, component.BatteryAlert);
            _alerts.ClearAlert(pilot, component.NoBatteryAlert);
            _alerts.ClearAlert(pilot, component.HealthAlert);
            _alerts.ClearAlert(pilot, component.BrokenAlert);

            // Unlock battery slot when unoccupied
            if (TryComp<ItemSlotsComponent>(uid, out var slots))
                _itemSlots.SetLock(uid, component.BatterySlotId, false, slots);
        }
        else if (args.Container == component.EquipmentContainer)
        {
            // Clear owner and raise removal event.
            if (!TryComp<MechEquipmentComponent>(args.Entity, out var eq))
                return;

            if (eq.EquipmentOwner == uid)
                eq.EquipmentOwner = null;
        }
    }

    private void OnRemoveBattery(EntityUid uid, MechComponent component, RemoveBatteryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        RemoveBattery(uid, component);
        _actionBlocker.UpdateCanMove(uid);

        args.Handled = true;

        UpdateUserInterface(uid, component);
    }

    private void OnMapInit(EntityUid uid, MechComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);

        foreach (var equipment in component.StartingEquipment)
        {
            var ent = Spawn(equipment, xform.Coordinates);
            InsertEquipment(uid, ent, component);
        }

        foreach (var module in component.StartingModules)
        {
            var ent = Spawn(module, xform.Coordinates);
            InsertEquipment(uid, ent, component);
        }

        // Ensure cabin pressure component is present for airtight operation
        EnsureComp<MechCabinAirComponent>(uid);

        component.Integrity = component.MaxIntegrity;
        component.Airtight = false;

        SetIntegrity(uid, component.MaxIntegrity, component);
        _actionBlocker.UpdateCanMove(uid);

        UpdateUserInterface(uid, component);
        UpdateHealthAlert((uid, component));
    }

    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        // UI can always be opened, access control is handled in the UI itself
        args.Handled = true;
        ToggleMechUi(uid, component);
    }

    private void OnToolUseAttempt(EntityUid uid, MechPilotComponent component, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target == component.Mech)
            args.Cancelled = true;
    }

    private void OnMechEntry(EntityUid uid, MechComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // Allow entry if locks are not active; block only if active and user lacks access
        if (TryComp<MechLockComponent>(uid, out var lockComp) && lockComp.IsLocked && !_lockSystem.CheckAccess(uid, args.User, lockComp))
        {
            _lockSystem.CheckAccessWithFeedback(uid, args.User, lockComp);
            return;
        }

        if (!Vehicle.CanOperate(uid, args.User))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter-popup", ("item", uid)), args.User);
            return;
        }

        TryInsert(uid, args.Args.User, component);
        args.Handled = true;

        UpdateUserInterface(uid, component);
        UpdateBatteryAlert((uid, component));
        UpdateHealthAlert((uid, component));
    }

    private void OnMechExit(EntityUid uid, MechComponent component, MechExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var pilot = Vehicle.GetOperatorOrNull(uid);

        TryEject(uid, component);

        args.Handled = true;

        UpdateUserInterface(uid, component);

        // Clear alerts after pilot exit
        if (pilot.HasValue)
        {
            _alerts.ClearAlert(pilot.Value, component.BatteryAlert);
            _alerts.ClearAlert(pilot.Value, component.NoBatteryAlert);
            _alerts.ClearAlert(pilot.Value, component.HealthAlert);
            _alerts.ClearAlert(pilot.Value, component.BrokenAlert);

            _actionBlocker.UpdateCanMove(pilot.Value);
        }
    }

    private void OnDamageChanged(EntityUid uid, MechComponent component, DamageChangedEvent args)
    {
        var integrity = component.MaxIntegrity - args.Damageable.TotalDamage;
        SetIntegrity(uid, integrity, component);

        // Sync construction graph with mech state
        var cc = EnsureComp<ConstructionComponent>(uid);
        if (component.Broken)
        {
            if (_construction.ChangeGraph(uid, null, "MechRepair", "start", performActions: false, cc))
                _construction.SetPathfindingTarget(uid, "repaired", cc);
        }

        UpdateUserInterface(uid, component);
        UpdateHealthAlert((uid, component));
    }

    private void OnBatteryChanged(EntityUid uid, MechComponent component, PowerCellChangedEvent args)
    {
        // Battery changed, update UI and alerts
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert((uid, component));
    }

    private void OnBatteryChanged(EntityUid uid, MechComponent component, PowerCellSlotEmptyEvent args)
    {
        // Battery removed, update UI and alerts
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert((uid, component));
    }

    private void ToggleMechUi(EntityUid uid, MechComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        user ??= Vehicle.GetOperatorOrNull(uid);
        if (user == null)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        // Open UI using UserInterfaceSystem
        _ui.TryToggleUi(uid, MechUiKey.Key, actor.PlayerSession);
    }

    public bool TryGetGasModuleAir(EntityUid mechUid, out GasMixture? air)
    {
        air = null;
        if (!TryComp<MechComponent>(mechUid, out var mech))
            return false;

        foreach (var ent in mech.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechAirTankModuleComponent>(ent, out _))
            {
                if (TryComp<GasTankComponent>(ent, out var tank))
                {
                    air = tank.Air;
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    public bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (delta > 0)
            return false;

        var amount = MathF.Abs(delta.Float());
        if (!_powerCell.TryUseCharge(uid, amount))
            return false;

        UpdateUserInterface(uid, component);
        UpdateBatteryAlert((uid, component));

        return true;
    }

    private void UpdateBatteryAlert(Entity<MechComponent> ent)
    {
        var pilot = ent.Comp.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        if (!_powerCell.TryGetBatteryFromSlot(ent, out var batt))
        {
            _alerts.ClearAlert(pilot.Value, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(pilot.Value, ent.Comp.NoBatteryAlert);
            return;
        }

        var max = MathF.Max(batt.MaxCharge, 0.0001f);
        var chargePercent = (short)MathF.Round(batt.CurrentCharge / max * 10f);

        // we make sure 0 only shows if they have absolutely no battery.
        // also account for floating point imprecision
        if (chargePercent == 0 && batt.CurrentCharge > 0)
            chargePercent = 1;

        _alerts.ClearAlert(pilot.Value, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(pilot.Value, ent.Comp.BatteryAlert, chargePercent);
    }

    private void UpdateHealthAlert(Entity<MechComponent> ent)
    {
        var pilot = ent.Comp.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        if (ent.Comp.Broken)
        {
            // Mech is broken
            _alerts.ClearAlert(pilot.Value, ent.Comp.HealthAlert);
            _alerts.ShowAlert(pilot.Value, ent.Comp.BrokenAlert);
        }
        else
        {
            // Mech is healthy, show health percentage
            _alerts.ClearAlert(pilot.Value, ent.Comp.BrokenAlert);

            var integrity = ent.Comp.Integrity.Float();
            var maxIntegrity = ent.Comp.MaxIntegrity.Float();
            var healthPercent = (short)MathF.Round((1f - integrity / maxIntegrity) * 4f);
            _alerts.ShowAlert(pilot.Value, ent.Comp.HealthAlert, healthPercent);
        }
    }

    public void RemoveBattery(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _container.EmptyContainer(component.BatterySlot);

        _actionBlocker.UpdateCanMove(uid);
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnMechBrokenSound(EntityUid uid, MechComponent component, MechBrokenSoundEvent args)
    {
        _audio.PlayPvs(args.Sound, uid);
    }

    public override bool CanInsert(EntityUid uid, EntityUid toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return base.CanInsert(uid, toInsert, component) && _actionBlocker.CanMove(toInsert);
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
            _popup.PopupEntity(Loc.GetString("mech-select-none-popup"), mech);
        }
        else
        {
            var equipment = GetEntity(args.Equipment);
            if (Exists(equipment) && mechComp.EquipmentContainer.ContainedEntities.Any(e => e == equipment))
            {
                mechComp.CurrentSelectedEquipment = equipment;
                _popup.PopupEntity(Loc.GetString("mech-select-popup", ("item", equipment)), mech);
            }
        }

        Dirty(mech, mechComp);
        RefreshPilotHandVirtualItems(mech, mechComp);
    }

    private void OnBeingGibbed(EntityUid uid, MechComponent component, ref BeingGibbedEvent args)
    {
        // Eject pilot if present
        if (component.PilotSlot.ContainedEntity != null)
        {
            TryEject(uid, component);
        }

        if (component.PilotSlot.ContainedEntity != null)
            args.GibbedParts.Add(component.PilotSlot.ContainedEntity.Value);

        // TODO: Parts should fall out
        QueueDel(uid);
    }
}
