using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.Rounding;
using Content.Shared.Temperature.HeatContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// Handles processing logic for the thermo-electric generator (TEG).
/// </summary>
/// <remarks>
/// <para>
/// The TEG generates power by exchanging heat between gases flowing through its two sides.
/// The gas flows through a "circulator" entity on each side, which have both an inlet and an outlet port.
/// </para>
/// <remarks>
/// Connecting the TEG core to its circulators is implemented via a node group. See <see cref="TegNodeGroup"/>.
/// </remarks>
/// <para>
/// The TEG center does HV power output, and must also be connected to an LV wire for the TEG to function.
/// </para>
/// <para>
/// Unlike in SS13, the TEG actually adjusts gas heat exchange to match the energy demand of the power network.
/// To achieve this, the TEG implements its own ramping logic instead of using the built-in Pow3r ramping.
/// The TEG actually has a maximum output of +n% more than was really generated,
/// which allows Pow3r to draw more power to "signal" that there is more network load.
/// The ramping is also exponential instead of linear like in normal Pow3r.
/// This system does mean a fully-loaded TEG creates +n% power out of thin air, but this is considered acceptable.
/// </para>
/// </remarks>
/// <seealso cref="TegGeneratorComponent"/>
/// <seealso cref="TegCirculatorComponent"/>
/// <seealso cref="TegNodeGroup"/>
/// <seealso cref="TegSensorData"/>
public sealed class TegSystem : EntitySystem
{
    /// <summary>
    /// Node name for the TEG part connection nodes (<see cref="TegNodeGroup"/>).
    /// </summary>
    private const string NodeNameTeg = "teg";

    /// <summary>
    /// Node name for the inlet pipe of a circulator.
    /// </summary>
    private const string NodeNameInlet = "inlet";

    /// <summary>
    /// Node name for the outlet pipe of a circulator.
    /// </summary>
    private const string NodeNameOutlet = "outlet";

    /// <summary>
    /// Device network command to have the TEG output a <see cref="TegSensorData"/> object for its last statistics.
    /// </summary>
    public const string DeviceNetworkCommandSyncData = "teg_sync_data";

    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TegGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<TegGeneratorComponent, PowerChangedEvent>(GeneratorPowerChange);
        SubscribeLocalEvent<TegGeneratorComponent, DeviceNetworkPacketEvent>(DeviceNetworkPacketReceived);

        SubscribeLocalEvent<TegGeneratorComponent, ExaminedEvent>(GeneratorExamined);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();

        // Downright diabolical and evil, but the TegSystem on the client does the same thing.
        SubscribeLocalEvent<TegCirculatorComponent, ExaminedEvent>(CirculatorExamined);
    }

    private void CirculatorExamined(Entity<TegCirculatorComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("teg-circulator-examine-temperature", ("temp", Math.Round(ent.Comp.HeatContainer.Temperature, 2))));
    }

    private void GeneratorExamined(EntityUid uid, TegGeneratorComponent component, ExaminedEvent args)
    {
        if (GetNodeGroup(uid) is not { IsFullyBuilt: true })
        {
            args.PushMarkup(Loc.GetString("teg-generator-examine-connection"));
        }
        else
        {
            var supplier = Comp<PowerSupplierComponent>(uid);

            using (args.PushGroup(nameof(TegGeneratorComponent)))
            {
                args.PushMarkup(Loc.GetString("teg-generator-examine-power", ("power", supplier.CurrentSupply)));
                args.PushMarkup(Loc.GetString("teg-generator-examine-power-max-output", ("power", supplier.MaxSupply)));
            }
        }
    }

    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        if (!powerReceiver.Powered)
        {
            supplier.MaxSupply = 0;
            return;
        }

        var tegGroup = GetNodeGroup(uid);
        if (tegGroup is not { IsFullyBuilt: true })
            return;

        var circA = tegGroup.CirculatorA!.Owner;
        var circB = tegGroup.CirculatorB!.Owner;

        var (inletA, outletA) = GetPipes(circA);
        var (inletB, outletB) = GetPipes(circB);

        var (airA, δpA) = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var (airB, δpB) = GetCirculatorAirTransfer(inletB.Air, outletB.Air);

        var cA = _atmosphere.GetHeatCapacity(airA, true);
        var cB = _atmosphere.GetHeatCapacity(airB, true);

        var circAComp = Comp<TegCirculatorComponent>(circA);
        var circBComp = Comp<TegCirculatorComponent>(circB);

        // We transfer heat between the circulator and the gas flowing through it.
        PerformCirculatorHeatExchange(cA, airA, circAComp, args.dt);
        PerformCirculatorHeatExchange(cB, airB, circBComp, args.dt);

        // Shift ramp position based on demand and generation from previous tick.
        var curRamp = component.RampPosition;
        var lastDraw = supplier.CurrentSupply;
        curRamp = MathHelper.Clamp(lastDraw, curRamp / component.RampFactor, curRamp * component.RampFactor);
        curRamp = MathF.Max(curRamp, component.RampMinimum);
        component.RampPosition = curRamp;

        var electricalEnergy = 0f;

        var circATemp = circAComp.HeatContainer.Temperature;
        var circBTemp = circBComp.HeatContainer.Temperature;

        var dT = Math.Abs(circATemp - circBTemp);

        // Don't bother doing anything if the \deltaT is too small.
        if (dT > component.TemperatureTolerance)
        {
            var (Thot, Tcold) = circATemp > circBTemp ? (circATemp, circBTemp) : (circBTemp, circATemp);

            // Establish our base thermal efficiency.
            var N = component.ThermalEfficiency;

            // Calculate Carnot efficiency
            var Nmax = 1 - Tcold / Thot;
            N = MathF.Min(N, Nmax); // clamp by Carnot efficiency

            // Reduce efficiency at low temperature differences to encourage burn chambers (instead
            // of just feeding the TEG room temperature gas from an infinite gas miner).
            N *= MathF.Tanh(dT/700); // https://www.wolframalpha.com/input?i=tanh(x/700)+from+0+to+1000

            // Calculate the desired amount of energy to generate this tick based on ramping above.
            // The TEG will generate only the energy it needs to satisfy demand
            // (and it'll only move the required amount of heat across the sides to achieve this).
            var targetEnergy = curRamp / args.dt;
            var desiredTransfer = targetEnergy / (N * component.PowerFactor);

            // Limit transfer to the maximum amount of energy we can generate this tick.
            var transfer = Math.Min(circAComp.HeatContainer.EquilibriumHeatQuery(ref circBComp.HeatContainer), desiredTransfer);

            electricalEnergy = transfer * N * component.PowerFactor;

            // Remember that Q_{in} = W + Q_{out}. Determine the amount of waste
            // heat we need to move to the cold side.
            var outTransfer = transfer * (1 - N);

            // Adjust thermal energy in transferred gas mixtures.
            if (circATemp > circBTemp)
            {
                // A -> B
                circAComp.HeatContainer.AddHeat(transfer);
                circBComp.HeatContainer.AddHeat(-outTransfer);
            }
            else
            {
                // B -> A
                circAComp.HeatContainer.AddHeat(-outTransfer);
                circBComp.HeatContainer.AddHeat(transfer);
            }
        }

        component.LastGeneration = electricalEnergy;

        // Turn energy (at atmos tick rate) into wattage.
        var power = electricalEnergy / args.dt;

        // Add ramp factor. This magics slight power into existence, but allows us to ramp up.
        supplier.MaxSupply = power * component.RampFactor;

        circAComp.LastPressureDelta = δpA;
        circAComp.LastMolesTransferred = airA.TotalMoles;
        circBComp.LastPressureDelta = δpB;
        circBComp.LastMolesTransferred = airB.TotalMoles;

        _atmosphere.Merge(outletA.Air, airA);
        _atmosphere.Merge(outletB.Air, airB);

        UpdateAppearance(uid, component, powerReceiver, tegGroup);
    }

    private void UpdateAppearance(
        EntityUid uid,
        TegGeneratorComponent component,
        ApcPowerReceiverComponent powerReceiver,
        TegNodeGroup nodeGroup)
    {
        int powerLevel;
        if (powerReceiver.Powered)
        {
            powerLevel = ContentHelpers.RoundToLevels(
                component.RampPosition - component.RampMinimum,
                component.MaxVisualPower - component.RampMinimum,
                12);
        }
        else
        {
            powerLevel = 0;
        }

        _ambientSound.SetAmbience(uid, powerLevel >= 1);
        // TODO: Ok so this introduces popping which is a major shame big rip.
        // _ambientSound.SetVolume(uid, MathHelper.Lerp(component.VolumeMin, component.VolumeMax, MathHelper.Clamp01(component.RampPosition / component.MaxVisualPower)));

        _appearance.SetData(uid, TegVisuals.PowerOutput, powerLevel);

        if (nodeGroup.IsFullyBuilt)
        {
            UpdateCirculatorAppearance(nodeGroup.CirculatorA!.Owner, powerReceiver.Powered);
            UpdateCirculatorAppearance(nodeGroup.CirculatorB!.Owner, powerReceiver.Powered);
        }
    }

    [Access(typeof(TegNodeGroup))]
    public void UpdateGeneratorConnectivity(
        EntityUid uid,
        TegNodeGroup group,
        TegGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);

        _receiver.SetPowerDisabled(uid, !group.IsFullyBuilt, powerReceiver);
        UpdateAppearance(uid, component, powerReceiver, group);
    }

    [Access(typeof(TegNodeGroup))]
    public void UpdateCirculatorConnectivity(
        EntityUid uid,
        TegNodeGroup group,
        TegCirculatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // If the group IS fully built, the generator will update its circulators.
        // Otherwise, make sure circulator is set to nothing.
        if (!group.IsFullyBuilt)
        {
            UpdateCirculatorAppearance((uid, component), false);
        }
    }

    private void UpdateCirculatorAppearance(Entity<TegCirculatorComponent?> ent, bool powered)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var circ = ent.Comp;

        TegCirculatorSpeed speed;
        if (powered && circ.LastPressureDelta > 0 && circ.LastMolesTransferred > 0)
        {
            if (circ.LastPressureDelta > circ.VisualSpeedDelta)
                speed = TegCirculatorSpeed.SpeedFast;
            else
                speed = TegCirculatorSpeed.SpeedSlow;
        }
        else
        {
            speed = TegCirculatorSpeed.SpeedStill;
        }

        _appearance.SetData(ent, TegVisuals.CirculatorSpeed, speed);
        _appearance.SetData(ent, TegVisuals.CirculatorPower, powered);

        if (_pointLight.TryGetLight(ent, out var pointLight))
        {
            _pointLight.SetEnabled(ent, powered, pointLight);
            _pointLight.SetColor(ent, speed == TegCirculatorSpeed.SpeedFast ? circ.LightColorFast : circ.LightColorSlow, pointLight);
        }
    }

    private void GeneratorPowerChange(EntityUid uid, TegGeneratorComponent component, ref PowerChangedEvent args)
    {
        // TODO: I wish power events didn't go out on shutdown.
        if (TerminatingOrDeleted(uid))
            return;

        var nodeGroup = GetNodeGroup(uid);
        if (nodeGroup == null)
            return;

        UpdateAppearance(uid, component, Comp<ApcPowerReceiverComponent>(uid), nodeGroup);
    }

    /// <returns>Null if the node group is not yet available. This can happen during initialization.</returns>
    private TegNodeGroup? GetNodeGroup(EntityUid uidGenerator)
    {
        NodeContainerComponent? nodeContainer = null;
        if (!_nodeContainerQuery.Resolve(uidGenerator, ref nodeContainer))
            return null;

        if (!nodeContainer.Nodes.TryGetValue(NodeNameTeg, out var tegNode))
            return null;

        if (tegNode.NodeGroup is not TegNodeGroup tegGroup)
            return null;

        return tegGroup;
    }

    private static (GasMixture, float δp) GetCirculatorAirTransfer(GasMixture airInlet, GasMixture airOutlet)
    {
        var n1 = airInlet.TotalMoles;
        var n2 = airOutlet.TotalMoles;
        var p1 = airInlet.Pressure;
        var p2 = airOutlet.Pressure;
        var V1 = airInlet.Volume;
        var V2 = airOutlet.Volume;
        var T1 = airInlet.Temperature;
        var T2 = airOutlet.Temperature;

        var δp = p1 - p2;

        var denom = T1 * V2 + T2 * V1;

        if (δp > 0 && p1 > 0 && denom > 0)
        {
            var transferMoles = n1 - (n1 + n2) * T2 * V1 / denom;
            return (airInlet.Remove(transferMoles), δp);
        }

        return (new GasMixture(), δp);
    }

    /// <summary>
    /// Preforms heat exchange between the circulator and the gas flowing through it.
    /// </summary>
    /// <param name="heatCapacity">The heat capacity of the circulator gas.</param>
    /// <param name="air">The GasMixture of the gas currently in the circulator.</param>
    /// <param name="comp">Comp of the <see cref="TegCirculatorComponent"/></param>
    /// <param name="dt">Atmos delta time</param>
    private static void PerformCirculatorHeatExchange(float heatCapacity,
        GasMixture air,
        TegCirculatorComponent comp,
        float dt)
    {
        // Prevent heat transfer if there isn't any gas in the circulator.
        // Also prevent heat transfer if the mixtures are effectively equal in temperature.
        var δt = air.Temperature - comp.HeatContainer.Temperature;
        if (air.TotalMoles == 0 || MathF.Abs(δt) < Atmospherics.MinimumTemperatureDeltaToConsider)
            return;

        // Heat transfer is calculated as
        var dQ = comp.ConductivityConstant * δt * dt;

        /*
        We need to prevent heat transfer beyond atmospherics Tmax, or else
        the TEG will violate thermodynamics.

        You can test this by attempting to run very small molar amounts of gas (ex. 0.003 mol plasma).
        If the sides do not rapidly start to equalize in temperature, you have formed a heat void.

        This is because Atmospherics caps heat at Atmospherics.Tmax, but the method will gladly
        transfer heat to the gas, effectively voiding it.
        */

        // This is the amount of heat that needs to be transferred to hit Tmax on the air.
        var temp = new HeatContainer(heatCapacity, Atmospherics.Tmax);
        var dQMax = temp.EquilibriumHeatQuery(ref comp.HeatContainer);

        // Clamp. This effectively clamps exchanger -> gas heat transfer, while allowing unrestricted
        // gas -> exchanger heat transfer.
        dQ = MathF.Max(dQ, dQMax);

        comp.HeatContainer.AddHeat(dQ);
        air.Temperature -= dQ / heatCapacity;
    }

    private (PipeNode inlet, PipeNode outlet) GetPipes(EntityUid uidCirculator)
    {
        var nodeContainer = _nodeContainerQuery.GetComponent(uidCirculator);
        var inlet = (PipeNode) nodeContainer.Nodes[NodeNameInlet];
        var outlet = (PipeNode) nodeContainer.Nodes[NodeNameOutlet];

        return (inlet, outlet);
    }

    private void DeviceNetworkPacketReceived(
        EntityUid uid,
        TegGeneratorComponent component,
        DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
            return;

        switch (cmd)
        {
            case DeviceNetworkCommandSyncData:
                var group = GetNodeGroup(uid);
                if (group is not { IsFullyBuilt: true })
                    return;

                var supplier = Comp<PowerSupplierComponent>(uid);

                var payload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = DeviceNetworkCommandSyncData,
                    [DeviceNetworkCommandSyncData] = new TegSensorData
                    {
                        CirculatorA = GetCirculatorSensorData(group.CirculatorA!.Owner),
                        CirculatorB = GetCirculatorSensorData(group.CirculatorB!.Owner),
                        LastGeneration = component.LastGeneration,
                        PowerOutput = supplier.CurrentSupply,
                        RampPosition = component.RampPosition
                    }
                };

                _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload);
                break;
        }
    }

    private TegSensorData.Circulator GetCirculatorSensorData(EntityUid circulator)
    {
        var (inlet, outlet) = GetPipes(circulator);

        var comp = Comp<TegCirculatorComponent>(circulator);

        return new TegSensorData.Circulator(
            inlet.Air.Pressure,
            outlet.Air.Pressure,
            inlet.Air.Temperature,
            outlet.Air.Temperature,
            comp.HeatContainer.Temperature);
    }
}
