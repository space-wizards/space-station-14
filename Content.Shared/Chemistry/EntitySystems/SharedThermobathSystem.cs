using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// The system for UI and heat transfer logic for a device that can heat or cool beakers.
/// </summary>
public abstract class SharedThermobathSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ThermoregulatorSystem _thermoregulator = default!;

    private EntityQuery<SolutionContainerManagerComponent> _solutionManagerQuery;

    private const string BeakerSlotId = "beakerSlot";
    private const string SolutionId = "beaker";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermobathComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ThermobathComponent, ThermobathPowerChangedMessage>(OnPowerChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, ThermobathSetpointChangedMessage>(OnSetpointChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, ThermobathModeChangedMessage>(OnModeChangeMessage);
        SubscribeLocalEvent<ThermobathComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<ThermobathComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<ThermobathComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ThermobathComponent, ThermoregulatorUpdatedEvent>(OnThermoregulatorUpdated);

        Subs.BuiEvents<ThermobathComponent>(ThermobathUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
        });

        _solutionManagerQuery = GetEntityQuery<SolutionContainerManagerComponent>();
    }

    private void OnThermoregulatorUpdated(Entity<ThermobathComponent> ent, ref ThermoregulatorUpdatedEvent args)
    {
        // Skip if not powered
        if (!_power.IsPowered(ent.Owner))
            return;

        // If we have a beaker then we want to transfer heat
        if (ent.Comp.HasBeaker && TryGetSolutionFromContainer(ent, out var soln, out var solution))
        {
            var solutionTemperature = solution.Temperature;
            var solutionHeatCapacity = solution.GetHeatCapacity(_proto);

            _thermoregulator.TransferHeatFromEntity((ent, args.Thermoregulator),
                solutionHeatCapacity,
                ref solutionTemperature);
            _solutionContainer.SetTemperature(soln.Value, solutionTemperature);
            ent.Comp.SolutionTemperature = solutionTemperature;

            DirtyField(ent.AsNullable(), nameof(ThermobathComponent.SolutionTemperature));
        }

        // Update the UI regardless since our temperature has changed
        UpdateUi(ent);
    }

    private void OnUiOpened(Entity<ThermobathComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnStartup(Entity<ThermobathComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, BeakerSlotId);
    }

    private void OnEntInsertedIntoContainer(Entity<ThermobathComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != BeakerSlotId)
            return;

        if (!TryGetSolutionFromContainer(ent, out _, out var solution))
            return;

        ent.Comp.HasBeaker = true;
        ent.Comp.SolutionTemperature = solution.Temperature;
        DirtyFields(ent.AsNullable(), null, nameof(ThermobathComponent.SolutionTemperature), nameof(ThermobathComponent.HasBeaker));
        UpdateUi(ent);
    }

    private void OnEntRemovedFromContainer(Entity<ThermobathComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != BeakerSlotId)
            return;

        ent.Comp.HasBeaker = args.Container.Count > 0;
        ent.Comp.SolutionTemperature = null;
        DirtyFields(ent.AsNullable(), null, nameof(ThermobathComponent.SolutionTemperature), nameof(ThermobathComponent.HasBeaker));
        UpdateUi(ent);
    }

    private void OnPowerChanged(Entity<ThermobathComponent> ent, ref PowerChangedEvent args)
    {
        _thermoregulator.SetEnabled(ent.Owner, args.Powered);
        UpdateUi(ent);
    }

    private void OnPowerChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathPowerChangedMessage args)
    {
        _power.TogglePower(ent, user: args.Actor);
        // Would be handled by OnPowerChanged but currently it's raised from the server
        _thermoregulator.SetEnabled(ent.Owner, args.Powered);
        UpdateUi(ent);
    }

    private void OnSetpointChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathSetpointChangedMessage args)
    {
        _thermoregulator.SetSetpoint(ent.Owner, args.Setpoint);
        UpdateUi(ent);
    }

    private void OnModeChangeMessage(Entity<ThermobathComponent> ent, ref ThermobathModeChangedMessage args)
    {
        _thermoregulator.SetMode(ent.Owner, args.Mode);
        UpdateUi(ent);
    }

    /// <summary>
    /// Helper method to fetch a solution from the container.
    /// </summary>
    private bool TryGetSolutionFromContainer(
        Entity<ThermobathComponent> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln,
        [NotNullWhen(true)] out Solution? solution)
    {
        soln = null;
        solution = null;

        if (!_container.TryGetContainer(ent, BeakerSlotId, out var beakerSlot))
            return false;

        foreach (var entity in beakerSlot.ContainedEntities)
        {
            if (!_solutionContainer.TryGetSolution((entity, _solutionManagerQuery.GetComponent(entity)), SolutionId, out soln, out solution))
                continue;

            return true;
        }

        return false;
    }

    protected virtual void UpdateUi(Entity<ThermobathComponent> ent)
    {
        // This is implemented in client system
    }
}
