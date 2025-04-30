using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract class SharedHotplateSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<SolutionContainerManagerComponent> _solutionManagerQuery;

    private const string BeakerSlotId  = "beakerSlot";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HotplateComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<HotplateComponent, HotplatePowerChangedMessage>(OnPowerChangeMessage);
        SubscribeLocalEvent<HotplateComponent, HotplateSetpointChangedMessage>(OnSetpointChangeMessage);
        SubscribeLocalEvent<HotplateComponent, HotplateModeChangedMessage>(OnModeChangeMessage);
        SubscribeLocalEvent<HotplateComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<HotplateComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<HotplateComponent, ComponentStartup>(OnStartup);

        Subs.BuiEvents<HotplateComponent>(HotplateUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
        });

        _solutionManagerQuery = GetEntityQuery<SolutionContainerManagerComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HotplateComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var hotplate, out var containerManager))
        {
            // Skip if no beaker inserted
            if (!hotplate.HasBeaker)
                continue;

            // Skip if not powered
            if (!_power.IsPowered(uid))
                continue;

            // Get beaker container
            if (!_container.TryGetContainer(uid, BeakerSlotId, out var beakerSlot, containerManager))
                continue;

            foreach (var entity in beakerSlot.ContainedEntities)
            {
                if (!_solutionManagerQuery.TryComp(entity, out var solutionManager))
                    continue;

                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutionManager)))
                {
                    UpdateBeakerTemperature((uid, hotplate), soln, frameTime);
                }
            }
        }
    }

    private void UpdateBeakerTemperature(Entity<HotplateComponent> hotplateEnt, Entity<SolutionComponent> solnEnt, float frameTime)
    {
        var T = solnEnt.Comp.Solution.Temperature;      // Current temperature
        var Ts = hotplateEnt.Comp.Setpoint;             // Temperature setpoint
        var H = hotplateEnt.Comp.Hysteresis;            // Hysteresis band
        var SB = hotplateEnt.Comp.ScaleBand;            // Power scaling range beyond hysteresis
        var PhMax = hotplateEnt.Comp.HeatingPower;      // Max heating power
        var PcMax = hotplateEnt.Comp.CoolingPower;      // Max cooling power

        // Determine heating/cooling state using hysteresis.
        // - Heating triggers below Ts - H
        // - Cooling triggers above Ts + H
        var heating = false;
        var cooling = false;

        switch (hotplateEnt.Comp.Mode)
        {
            case HotplateMode.Auto:
                if (hotplateEnt.Comp.ActiveState == HotplateActiveState.Heating)
                {
                    // Stop heating when we reach or exceed the setpoint
                    heating = T < Ts;
                }
                else if (T <= Ts - H)
                {
                    heating = true;
                }

                if (hotplateEnt.Comp.ActiveState == HotplateActiveState.Cooling)
                {
                    // Stop cooling when we go below the setpoint
                    cooling = T > Ts;
                }
                else if (T >= Ts + H)
                {
                    cooling = true;
                }
                break;

            case HotplateMode.Heating:
                if (hotplateEnt.Comp.ActiveState == HotplateActiveState.Heating)
                    heating = T < Ts;
                else if (T <= Ts - H)
                    heating = true;
                break;

            case HotplateMode.Cooling:
                if (hotplateEnt.Comp.ActiveState == HotplateActiveState.Cooling)
                    cooling = T > Ts;
                else if (T >= Ts + H)
                    cooling = true;
                break;
        }

        // Compute distance from the setpoint
        var distance = MathF.Abs(T - Ts);

        // Compute a power scale between 0 and 1 using a nonlinear (quadratic) ramp:
        // This provides a smooth curve: very low power near the hysteresis threshold,
        // and high power as you get farther from the setpoint.
        var raw = Math.Clamp((distance - H) / (SB - H), 0f, 1f);
        var scale = raw * raw; // quadratic response

        // If we're heating or cooling and the scaled power is too low (near zero),
        // clamp to a small minimum (e.g. 10%) so that the system doesn't stall just outside the deadband.
        if ((heating || cooling) && scale < 0.1f)
            scale = 0.1f;

        // Calculate effective heating or cooling power
        var heatPower = heating ? PhMax * scale : 0f;
        var coolPower = cooling ? PcMax * scale : 0f;

        // Compute net power (positive = heating, negative = cooling)
        var netPower = heatPower - coolPower;

        // Convert net power (watts) to energy (joules) applied this frame
        // Q = P × Δt
        var energy = netPower * frameTime;

        // Apply energy to the solution (Q = P × Δt)
        _solutionContainer.AddThermalEnergy(solnEnt, energy);

        // Update visual/active state for UI
        var newState = HotplateActiveState.Idle;
        if (heating)
            newState = HotplateActiveState.Heating;
        else if (cooling)
            newState = HotplateActiveState.Cooling;

        if (!MathHelper.CloseTo(hotplateEnt.Comp.CurrentTemperature, solnEnt.Comp.Solution.Temperature))
        {
            hotplateEnt.Comp.CurrentTemperature = solnEnt.Comp.Solution.Temperature;
            DirtyField(hotplateEnt.AsNullable(), nameof(HotplateComponent.CurrentTemperature));
            UpdateUi(hotplateEnt);
        }

        if (hotplateEnt.Comp.ActiveState != newState)
        {
            hotplateEnt.Comp.ActiveState = newState;
            DirtyField(hotplateEnt.AsNullable(), nameof(HotplateComponent.ActiveState));
            UpdateUi(hotplateEnt);
        }
    }


    private void OnUiOpened(Entity<HotplateComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnStartup(Entity<HotplateComponent> ent, ref ComponentStartup args)
    {
        // Ensure the beaker container exists
        _container.EnsureContainer<ContainerSlot>(ent, BeakerSlotId);
    }

    private void OnEntInsertedIntoContainer(Entity<HotplateComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != BeakerSlotId)
            return;

        ent.Comp.HasBeaker = args.Container.Count > 0;
        DirtyField(ent.AsNullable(), nameof(HotplateComponent.HasBeaker));
        UpdateUi(ent);
    }

    private void OnEntRemovedFromContainer(Entity<HotplateComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != BeakerSlotId)
            return;

        ent.Comp.HasBeaker = args.Container.Count > 0;
        ent.Comp.ActiveState = HotplateActiveState.Idle;
        DirtyFields(ent.AsNullable(), null, nameof(HotplateComponent.HasBeaker), nameof(HotplateComponent.ActiveState));
        UpdateUi(ent);
    }

    private void OnPowerChanged(Entity<HotplateComponent> ent, ref PowerChangedEvent args)
    {
        ent.Comp.ActiveState = HotplateActiveState.Idle;
        DirtyField(ent.AsNullable(), nameof(HotplateComponent.ActiveState));
        UpdateUi(ent);
    }

    private void OnPowerChangeMessage(Entity<HotplateComponent> ent, ref HotplatePowerChangedMessage args)
    {
        _power.TogglePower(ent, user: args.Actor);
        UpdateUi(ent);
    }

    private void OnSetpointChangeMessage(Entity<HotplateComponent> ent, ref HotplateSetpointChangedMessage args)
    {
        ent.Comp.Setpoint = Math.Clamp(args.Setpoint, ent.Comp.MinTemperature, ent.Comp.MaxTemperature);
        DirtyField(ent.AsNullable(), nameof(HotplateComponent.Setpoint));
        UpdateUi(ent);
    }

    private void OnModeChangeMessage(Entity<HotplateComponent> ent, ref HotplateModeChangedMessage args)
    {
        ent.Comp.Mode = args.Mode;
        DirtyField(ent.AsNullable(), nameof(HotplateComponent.Mode));
        UpdateUi(ent);
    }

    protected virtual void UpdateUi(Entity<HotplateComponent> ent)
    {
        // This is implemented in client system
    }
}
