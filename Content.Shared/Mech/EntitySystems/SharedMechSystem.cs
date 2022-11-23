using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Body.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;
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
        SubscribeLocalEvent<SharedMechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);

        SubscribeLocalEvent<SharedMechComponent, InteractNoHandEvent>(RelayInteractionEvent);
        SubscribeLocalEvent<SharedMechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SharedMechComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnToggleEquipmentAction(EntityUid uid, SharedMechComponent component, MechToggleEquipmentEvent args)
    {
        CycleEquipment(uid);
    }

    private void RelayInteractionEvent<TEvent>(EntityUid uid, SharedMechComponent component, TEvent args) where TEvent : notnull
    {
        foreach (var module in component.EquipmentContainer.ContainedEntities)
        {
            RaiseLocalEvent(module, args);
        }
    }

    private void OnStartup(EntityUid uid, SharedMechComponent component, ComponentStartup args)
    {
        component.PilotSlot = _container.EnsureContainer<ContainerSlot>(uid, component.PilotSlotId);
        component.EquipmentContainer = _container.EnsureContainer<Container>(uid, component.EquipmentContainerId);
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

        _actions.AddAction(pilot, new InstantAction(_prototype.Index<InstantActionPrototype>(component.MechToggleAction)), mech);
        _actions.AddAction(pilot, new InstantAction(_prototype.Index<InstantActionPrototype>(component.MechUiAction)), mech);
    }

    private void RemoveUser(EntityUid mech, EntityUid pilot)
    {
        if (!RemComp<MechPilotComponent>(pilot))
            return;
        RemComp<RelayInputMoverComponent>(pilot);
        RemComp<InteractionRelayComponent>(pilot);

        _actions.RemoveProvidedActions(pilot, mech);
    }

    public void BreakMech(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TryEject(uid, component);
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
        if (equipmentIndex >= allEquipment.Count)
        {
            component.CurrentSelectedEquipment = null;
        }
        else
        {
            component.CurrentSelectedEquipment = allEquipment[equipmentIndex];
        }

        var popupString = component.CurrentSelectedEquipment != null
            ? Loc.GetString("mech-equipment-select-popup", ("item", component.CurrentSelectedEquipment))
            : Loc.GetString("mech-equipment-select-none-popup");

        if (_timing.IsFirstTimePredicted)
            _popup.PopupEntity(popupString, uid, Filter.Pvs(uid));
    }

    public bool TryChangeEnergy(EntityUid uid, float delta, SharedMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Energy + delta < 0)
            return false;

        component.Energy = Math.Clamp(component.Energy + delta, 0, component.MaxEnergy);
        Dirty(component);
        UpdateUserInterface(uid, component);
        return true;
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

    public virtual void UpdateUserInterface(EntityUid uid, SharedMechComponent component)
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

    private void UpdateAppearance(EntityUid uid, SharedMechComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, MechVisuals.Open, IsEmpty(component), appearance);
        _appearance.SetData(uid, MechVisuals.Broken, component.Broken, appearance);
    }
}
