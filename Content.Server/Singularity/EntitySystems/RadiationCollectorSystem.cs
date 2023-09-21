using Content.Server.Singularity.Components;
using Content.Shared.Interaction;
using Content.Shared.Singularity.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Radiation.Events;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Server.Atmos.Components;
using Content.Shared.Examine;
using Content.Server.Atmos;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Singularity.EntitySystems;

public sealed class RadiationCollectorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationCollectorComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RadiationCollectorComponent, OnIrradiatedEvent>(OnRadiation);
        SubscribeLocalEvent<RadiationCollectorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RadiationCollectorComponent, GasAnalyzerScanEvent>(OnAnalyzed);
    }

    private bool TryGetLoadedGasTank(EntityUid uid, [NotNullWhen(true)] out GasTankComponent? gasTankComponent)
    {
        gasTankComponent = null;
        var container = _containerSystem.EnsureContainer<ContainerSlot>(uid, "GasTank");

        if (container.ContainedEntity == null)
            return false;

        if (!EntityManager.TryGetComponent(container.ContainedEntity, out gasTankComponent))
            return false;

        return true;
    }

    private void OnInteractHand(EntityUid uid, RadiationCollectorComponent component, InteractHandEvent args)
    {
        var curTime = _gameTiming.CurTime;

        if (curTime < component.CoolDownEnd)
            return;

        ToggleCollector(uid, args.User, component);
        component.CoolDownEnd = curTime + component.Cooldown;
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
            float reactantMol = gasTankComponent.Air.GetMoles(gas.Reactant);
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
                gasTankComponent.Air.AdjustMoles(gas.Reactant, -Math.Min(delta, reactantMol));
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
            batteryComponent.CurrentCharge += charge;
        }
    }

    private void OnExamined(EntityUid uid, RadiationCollectorComponent component, ExaminedEvent args)
    {
        if (!TryGetLoadedGasTank(uid, out var gasTankComponent))
        {
            args.PushMarkup(Loc.GetString("power-radiation-collector-gas-tank-missing"));
            return;
        }

        args.PushMarkup(Loc.GetString("power-radiation-collector-gas-tank-present"));

        if (gasTankComponent.IsLowPressure)
        {
            args.PushMarkup(Loc.GetString("power-radiation-collector-gas-tank-low-pressure"));
        }
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
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = enabled;

        // Show message to the player
        if (user != null)
        {
            var msg = component.Enabled ? "radiation-collector-component-use-on" : "radiation-collector-component-use-off";
            _popupSystem.PopupEntity(Loc.GetString(msg), uid);
        }

        // Update appearance
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, RadiationCollectorComponent? component, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance))
            return;

        var state = component.Enabled ? RadiationCollectorVisualState.Active : RadiationCollectorVisualState.Deactive;
        _appearance.SetData(uid, RadiationCollectorVisuals.VisualState, state, appearance);
    }
}
