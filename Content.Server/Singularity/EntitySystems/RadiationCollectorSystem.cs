using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Shared.Atmos;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Radiation.Events;
using Content.Shared.Singularity.Components;
using Content.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.Singularity.EntitySystems;

public sealed class RadiationCollectorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;

    private const string GasTankContainer = "gas_tank";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationCollectorComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RadiationCollectorComponent, OnIrradiatedEvent>(OnRadiation);
        SubscribeLocalEvent<RadiationCollectorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RadiationCollectorComponent, GasAnalyzerScanEvent>(OnAnalyzed);
        SubscribeLocalEvent<RadiationCollectorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RadiationCollectorComponent, EntInsertedIntoContainerMessage>(OnTankChanged);
        SubscribeLocalEvent<RadiationCollectorComponent, EntRemovedFromContainerMessage>(OnTankChanged);
    }

    private bool TryGetLoadedGasTank(EntityUid uid, [NotNullWhen(true)] out GasTankComponent? gasTankComponent)
    {
        gasTankComponent = null;

        if (!_containerSystem.TryGetContainer(uid, GasTankContainer, out var container) || container.ContainedEntities.Count == 0)
            return false;

        if (!EntityManager.TryGetComponent(container.ContainedEntities.First(), out gasTankComponent))
            return false;

        return true;
    }

    private void OnMapInit(EntityUid uid, RadiationCollectorComponent component, MapInitEvent args)
    {
        TryGetLoadedGasTank(uid, out var gasTank);
        UpdateTankAppearance(uid, component, gasTank);
    }

    private void OnTankChanged(EntityUid uid, RadiationCollectorComponent component, ContainerModifiedMessage args)
    {
        TryGetLoadedGasTank(uid, out var gasTank);
        UpdateTankAppearance(uid, component, gasTank);
    }

    private void OnInteractHand(EntityUid uid, RadiationCollectorComponent component, InteractHandEvent args)
    {
        if (TryComp(uid, out UseDelayComponent? useDelay) && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        ToggleCollector(uid, args.User, component);
    }

    private void OnRadiation(EntityUid uid, RadiationCollectorComponent component, OnIrradiatedEvent args)
    {
        if (!component.Enabled || component.RadiationReactiveGases == null)
            return;

        if (!TryGetLoadedGasTank(uid, out var gasTankComponent))
            return;

        var charge = 0f;

        foreach (var gas in component.RadiationReactiveGases)
        {
            float reactantMol = gasTankComponent.Air.GetMoles(gas.ReactantPrototype);
            float delta = args.TotalRads * reactantMol * gas.ReactantBreakdownRate;

            // We need to offset the huge power gains possible when using very cold gases
            // (they allow you to have a much higher molar concentrations of gas in the tank).
            // Hence power output is modified using the Michaelis-Menten equation,
            // it will heavily penalise the power output of low temperature reactions:
            // 300K = 100% power output, 73K = 49% power output, 1K = 1% power output
            float temperatureMod = 1.5f * gasTankComponent.Air.Temperature / (150f + gasTankComponent.Air.Temperature);
            charge += args.TotalRads * reactantMol * component.ChargeModifier * gas.PowerGenerationEfficiency * temperatureMod;

            if (delta > 0)
            {
                gasTankComponent.Air.AdjustMoles(gas.ReactantPrototype, -Math.Min(delta, reactantMol));
            }

            if (gas.Byproduct != null)
            {
                gasTankComponent.Air.AdjustMoles((int) gas.Byproduct, delta * gas.MolarRatio);
            }
        }

        // No idea if this is even vaguely accurate to the previous logic.
        // The maths is copied from that logic even though it works differently.
        // But the previous logic would also make the radiation collectors never ever stop providing energy.
        // And since frameTime was used there, I'm assuming that this is what the intent was.
        // This still won't stop things being potentially hilariously unbalanced though.
        if (TryComp<BatteryComponent>(uid, out var batteryComponent))
        {
            _batterySystem.SetCharge(uid, charge, batteryComponent);
        }

        // Update appearance
        UpdatePressureIndicatorAppearance(uid, component, gasTankComponent);
    }

    private void OnExamined(EntityUid uid, RadiationCollectorComponent component, ExaminedEvent args)
    {
        if (!TryGetLoadedGasTank(uid, out var gasTank))
        {
            args.PushMarkup(Loc.GetString("power-radiation-collector-gas-tank-missing"));
            return;
        }

        args.PushMarkup(Loc.GetString("power-radiation-collector-gas-tank-present"));
    }

    private void OnAnalyzed(EntityUid uid, RadiationCollectorComponent component, GasAnalyzerScanEvent args)
    {
        if (!TryGetLoadedGasTank(uid, out var gasTankComponent))
            return;

        args.GasMixtures = new Dictionary<string, GasMixture?> { { Name(uid), gasTankComponent.Air } };
    }

    public void ToggleCollector(EntityUid uid, EntityUid? user = null, RadiationCollectorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetCollectorEnabled(uid, !component.Enabled, user, component);
    }

    public void SetCollectorEnabled(EntityUid uid, bool enabled, EntityUid? user = null, RadiationCollectorComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Enabled = enabled;

        // Show message to the player
        if (user != null)
        {
            var msg = component.Enabled ? "radiation-collector-component-use-on" : "radiation-collector-component-use-off";
            _popupSystem.PopupEntity(Loc.GetString(msg), uid);
        }

        // Update appearance
        UpdateMachineAppearance(uid, component);
    }

    private void UpdateMachineAppearance(EntityUid uid, RadiationCollectorComponent component, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;

        var state = component.Enabled ? RadiationCollectorVisualState.Active : RadiationCollectorVisualState.Deactive;
        _appearance.SetData(uid, RadiationCollectorVisuals.VisualState, state, appearance);
    }

    private void UpdatePressureIndicatorAppearance(EntityUid uid, RadiationCollectorComponent component, GasTankComponent? gasTank = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (gasTank == null || gasTank.Air.Pressure < 10)
            _appearance.SetData(uid, RadiationCollectorVisuals.PressureState, 0, appearance);

        else if (gasTank.Air.Pressure < Atmospherics.OneAtmosphere)
            _appearance.SetData(uid, RadiationCollectorVisuals.PressureState, 1, appearance);

        else if (gasTank.Air.Pressure < 3f * Atmospherics.OneAtmosphere)
            _appearance.SetData(uid, RadiationCollectorVisuals.PressureState, 2, appearance);

        else
            _appearance.SetData(uid, RadiationCollectorVisuals.PressureState, 3, appearance);
    }

    private void UpdateTankAppearance(EntityUid uid, RadiationCollectorComponent component, GasTankComponent? gasTank = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, RadiationCollectorVisuals.TankInserted, gasTank != null, appearance);

        UpdatePressureIndicatorAppearance(uid, component, gasTank, appearance);
    }
}
