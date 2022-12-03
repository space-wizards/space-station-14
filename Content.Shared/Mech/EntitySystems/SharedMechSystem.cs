using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mech.EntitySystems;

public abstract class SharedMechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SharedMechComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<MechPilotComponent, ComponentGetState>(OnPilotGetState);
        SubscribeLocalEvent<MechPilotComponent, ComponentHandleState>(OnPilotHandleState);

        SubscribeLocalEvent<SharedMechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);
        SubscribeLocalEvent<SharedMechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);
        SubscribeLocalEvent<SharedMechComponent, InteractNoHandEvent>(RelayInteractionEvent);
        SubscribeLocalEvent<SharedMechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SharedMechComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<MechPilotComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<MechPilotComponent, CanAttackFromContainerEvent>(OnCanAttackFromContainer);
    }

    #region State Handling
    private void OnHandleState(EntityUid uid, SharedMechComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MechComponentState state)
            return;

        component.Integrity = state.Integrity;
        component.MaxIntegrity = state.MaxIntegrity;
        component.Energy = state.Energy;
        component.MaxEnergy = state.MaxEnergy;
        component.CurrentSelectedEquipment = state.CurrentSelectedEquipment;
        component.Broken = state.Broken;
    }

    private void OnPilotGetState(EntityUid uid, MechPilotComponent component, ref ComponentGetState args)
    {
        args.State = new MechPilotComponentState
        {
            Mech = component.Mech
        };
    }

    private void OnPilotHandleState(EntityUid uid, MechPilotComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MechPilotComponentState state)
            return;

        component.Mech = state.Mech;
    }
    #endregion

    private void OnToggleEquipmentAction(EntityUid uid, SharedMechComponent component, MechToggleEquipmentEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        CycleEquipment(uid);
    }

    private void OnEjectPilotEvent(EntityUid uid, SharedMechComponent component, MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        TryEject(uid, component);
    }

    private void RelayInteractionEvent(EntityUid uid, SharedMechComponent component, InteractNoHandEvent args)
    {
        var pilot = component.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (component.CurrentSelectedEquipment != null)
        {
            RaiseLocalEvent(component.CurrentSelectedEquipment.Value, args);
        }
    }

    private void OnStartup(EntityUid uid, SharedMechComponent component, ComponentStartup args)
    {
        component.PilotSlot = _container.EnsureContainer<ContainerSlot>(uid, component.PilotSlotId);
        component.EquipmentContainer = _container.EnsureContainer<Container>(uid, component.EquipmentContainerId);
        component.BatterySlot = _container.EnsureContainer<ContainerSlot>(uid, component.BatterySlotId);
        UpdateAppearance(uid, component);
    }

    private void OnDestruction(EntityUid uid, SharedMechComponent component, DestructionEventArgs args)
    {
        BreakMech(uid, component);
    }

    private void SetupUser(EntityUid mech, EntityUid pilot, SharedMechComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var rider = EnsureComp<MechPilotComponent>(pilot);
        var relay = EnsureComp<RelayInputMoverComponent>(pilot);
        var irelay = EnsureComp<InteractionRelayComponent>(pilot);

        _mover.SetRelay(pilot, mech, relay);
        _interaction.SetRelay(pilot, mech, irelay);
        rider.Mech = mech;
        Dirty(rider);

        _actions.AddAction(pilot, new InstantAction(_prototype.Index<InstantActionPrototype>(component.MechCycleAction)), mech);
        _actions.AddAction(pilot, new InstantAction(_prototype.Index<InstantActionPrototype>(component.MechUiAction)), mech);
        _actions.AddAction(pilot, new InstantAction(_prototype.Index<InstantActionPrototype>(component.MechEjectAction)), mech);
    }

    private void RemoveUser(EntityUid mech, EntityUid pilot)
    {
        if (!RemComp<MechPilotComponent>(pilot))
            return;
        RemComp<RelayInputMoverComponent>(pilot);
        RemComp<InteractionRelayComponent>(pilot);

        _actions.RemoveProvidedActions(pilot, mech);
    }

    public virtual void BreakMech(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TryEject(uid, component);
        var equipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
        {
            RemoveEquipment(uid, ent, component);
        }

        component.Broken = true;
        UpdateAppearance(uid, component);
    }

    public void CycleEquipment(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var allEquipment = component.EquipmentContainer.ContainedEntities.ToList();

        var equipmentIndex = -1;
        if (component.CurrentSelectedEquipment != null)
        {
            bool StartIndex(EntityUid u) => u == component.CurrentSelectedEquipment;
            equipmentIndex = allEquipment.FindIndex(StartIndex);
        }

        equipmentIndex++;
        component.CurrentSelectedEquipment = equipmentIndex >= allEquipment.Count
            ? null
            : allEquipment[equipmentIndex];

        var popupString = component.CurrentSelectedEquipment != null
            ? Loc.GetString("mech-equipment-select-popup", ("item", component.CurrentSelectedEquipment))
            : Loc.GetString("mech-equipment-select-none-popup");

        if (_timing.IsFirstTimePredicted)
            _popup.PopupEntity(popupString, uid, Filter.Pvs(uid));
        Dirty(component);
    }

    public void InsertEquipment(EntityUid uid, EntityUid toInsert, SharedMechComponent? component = null, MechEquipmentComponent? equipmentComponent = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Resolve(toInsert, ref equipmentComponent))
            return;

        equipmentComponent.EquipmentOwner = uid;
        component.EquipmentContainer.Insert(toInsert, EntityManager);
        var ev = new MechEquipmentInsertedEvent(uid);
        RaiseLocalEvent(toInsert, ref ev);
        UpdateUserInterface(uid, component);
    }

    public void RemoveEquipment(EntityUid uid, EntityUid toRemove, SharedMechComponent? component = null, MechEquipmentComponent? equipmentComponent = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Resolve(toRemove, ref equipmentComponent))
            return;

        equipmentComponent.EquipmentOwner = null;
        component.EquipmentContainer.Remove(toRemove, EntityManager);
        var ev = new MechEquipmentRemovedEvent(uid);
        RaiseLocalEvent(toRemove, ref ev);

        if (component.CurrentSelectedEquipment == toRemove)
            CycleEquipment(uid, component);

        UpdateUserInterface(uid, component);
    }

    public virtual bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Energy + delta < 0)
            return false;

        component.Energy = FixedPoint2.Clamp(component.Energy + delta, 0, component.MaxEnergy);
        Dirty(component);
        UpdateUserInterface(uid, component);
        return true;
    }

    public void SetIntegrity(EntityUid uid, FixedPoint2 value, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Integrity = FixedPoint2.Clamp(value, 0, component.MaxIntegrity);

        if (component.Integrity <= 0)
        {
            BreakMech(uid, component);
        }
        else if (component.Broken)
        {
            component.Broken = false;
            UpdateAppearance(uid, component);
        }

        Dirty(component);
        UpdateUserInterface(uid, component);
    }

    public bool IsEmpty(SharedMechComponent component)
    {
        return component.PilotSlot.ContainedEntity == null;
    }

    public bool CanInsert(EntityUid uid, EntityUid toInsert, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component) && _actionBlocker.CanMove(toInsert) && HasComp<BodyComponent>(toInsert);
    }

    /// <remarks>
    /// This is defined here so that UI updates can be accessed from shared.
    /// </remarks>
    public virtual void UpdateUserInterface(EntityUid uid, SharedMechComponent? component = null)
    {

    }

    public virtual bool TryInsert(EntityUid uid, EntityUid? toInsert, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (toInsert == null || component.PilotSlot.ContainedEntity == toInsert)
            return false;

        if (!CanInsert(uid, toInsert.Value, component))
            return false;

        SetupUser(uid, toInsert.Value);
        component.PilotSlot.Insert(toInsert.Value, EntityManager);
        UpdateAppearance(uid, component);
        return true;
    }

    public virtual bool TryEject(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.PilotSlot.ContainedEntity == null)
            return false;

        var pilot = component.PilotSlot.ContainedEntity.Value;

        RemoveUser(uid, pilot);
        _container.RemoveEntity(uid, pilot);
        UpdateAppearance(uid, component);
        return true;
    }

    private void OnGetMeleeWeapon(EntityUid uid, MechPilotComponent component, GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SharedMechComponent>(component.Mech, out var mech))
            return;

        var weapon = mech.CurrentSelectedEquipment ?? component.Mech;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnCanAttackFromContainer(EntityUid uid, MechPilotComponent component, CanAttackFromContainerEvent args)
    {
        args.CanAttack = true;
    }

    private void UpdateAppearance(EntityUid uid, SharedMechComponent ? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, MechVisuals.Open, IsEmpty(component), appearance);
        _appearance.SetData(uid, MechVisuals.Broken, component.Broken, appearance);
    }
}
