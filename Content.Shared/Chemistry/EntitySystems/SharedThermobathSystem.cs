using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Handles thermobath UI messages and appearance data.
/// </summary>
public abstract partial class SharedThermobathSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedThermoregulatorSystem _thermoregulator = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ThermobathComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnEntInsertedIntoContainer(Entity<ThermobathComponent> ent, ref EntInsertedIntoContainerMessage args) =>
        OnContainerModified(ent, args);

    [SubscribeLocalEvent]
    private void OnEntRemovedFromContainer(Entity<ThermobathComponent> ent, ref EntRemovedFromContainerMessage args) =>
        OnContainerModified(ent, args);

    private void OnContainerModified(Entity<ThermobathComponent> ent, ContainerModifiedMessage args)
    {
        if (args.Container.ID != ThermobathComponent.BeakerSlotId)
            return;

        UpdateUi(ent);

        if (_timing.ApplyingState)
            return;

        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<ThermobathComponent> ent, ref PowerChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        UpdateUi(ent);
        UpdateAppearance(ent, powered: args.Powered);
    }

    [SubscribeLocalEvent]
    private void OnTogglePowerMessage(Entity<ThermobathComponent> ent, ref ThermobathTogglePowerMessage args)
    {
        var powerEnabled = _power.TogglePower(ent, user: args.Actor);
        var powered = powerEnabled && _power.IsPowered(ent.Owner);

        UpdateUi(ent);
        UpdateAppearance(ent, powered: powered);
    }

    [SubscribeLocalEvent]
    private void OnSetpointChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathSetpointChangedMessage args)
    {
        _thermoregulator.SetSetpoint(ent.Owner, args.Setpoint);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnModeChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathModeChangedMessage args)
    {
        _thermoregulator.SetMode(ent.Owner, args.Mode);
        UpdateUi(ent);
    }

    private bool HasBeaker(EntityUid uid) =>
        _itemSlots.GetItemOrNull(uid, ThermobathComponent.BeakerSlotId) != null;

    protected void UpdateAppearance(
        Entity<ThermobathComponent> ent,
        ThermoregulatorComponent? thermoregulator = null,
        bool? powered = null)
    {
        var isPowered = powered ?? _power.IsPowered(ent.Owner);
        thermoregulator ??= CompOrNull<ThermoregulatorComponent>(ent);

        var activeMode = isPowered
            ? thermoregulator?.ActiveMode ?? ThermoregulatorActiveMode.Idle
            : ThermoregulatorActiveMode.Idle;

        _appearance.SetData(ent, ThermobathVisuals.Powered, isPowered);
        _appearance.SetData(ent, ThermobathVisuals.HasBeaker, HasBeaker(ent));
        _appearance.SetData(ent, ThermobathVisuals.ActiveMode, activeMode);
    }

    protected virtual void UpdateUi(Entity<ThermobathComponent> ent) { }
}
