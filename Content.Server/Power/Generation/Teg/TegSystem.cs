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
using Content.Shared.Examine;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.Rounding;
using Robust.Server.GameObjects;

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

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TegGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<TegGeneratorComponent, PowerChangedEvent>(GeneratorPowerChange);
        SubscribeLocalEvent<TegGeneratorComponent, DeviceNetworkPacketEvent>(DeviceNetworkPacketReceived);

        SubscribeLocalEvent<TegGeneratorComponent, ExaminedEvent>(GeneratorExamined);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
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
            args.PushMarkup(Loc.GetString("teg-generator-examine-power", ("power", supplier.CurrentSupply)));
        }
    }

    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var tegGroup = GetNodeGroup(uid);
        if (tegGroup is not { IsFullyBuilt: true })
            return;

        var supplier = Comp<PowerSupplierComponent>(uid);
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        if (!powerReceiver.Powered)
        {
            supplier.MaxSupply = 0;
            return;
        }

        var circA = tegGroup.CirculatorA!.Owner;
        var circB = tegGroup.CirculatorB!.Owner;

        var (inletA, outletA) = GetPipes(circA);
        var (inletB, outletB) = GetPipes(circB);

        var (airA, δpA) = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var (airB, δpB) = GetCirculatorAirTransfer(inletB.Air, outletB.Air);

        var cA = _atmosphere.GetHeatCapacity(airA, true);
        var cB = _atmosphere.GetHeatCapacity(airB, true);

        // Shift ramp position based on demand and generation from previous tick.
        var curRamp = component.RampPosition;
        var lastDraw = supplier.CurrentSupply;
        curRamp = MathHelper.Clamp(lastDraw, curRamp / component.RampFactor, curRamp * component.RampFactor);
        curRamp = MathF.Max(curRamp, component.RampMinimum);
        component.RampPosition = curRamp;

        var electricalEnergy = 0f;

        if (airA.Pressure > 0 && airB.Pressure > 0)
        {
            var hotA = airA.Temperature > airB.Temperature;

            // Calculate thermal and electrical energy transfer between the two sides.
            // Assume temperature equalizes, i.e. Ta*cA + Tb*cB = Tf*(cA+cB)
            var Tf = (airA.Temperature * cA + airB.Temperature * cB) / (cA + cB);
            // The maximum energy we can extract is (Ta - Tf)*cA, which is equal to (Tf - Tb)*cB
            var Wmax = MathF.Abs(airA.Temperature - Tf) * cA;

            var N = component.ThermalEfficiency;

            // Calculate Carnot efficiency
            var Thot = hotA ? airA.Temperature : airB.Temperature;
            var Tcold = hotA ? airB.Temperature : airA.Temperature;
            var Nmax = 1 - Tcold / Thot;
            N = MathF.Min(N, Nmax); // clamp by Carnot efficiency

            // Reduce efficiency at low temperature differences to encourage burn chambers (instead
            // of just feeding the TEG room temperature gas from an infinite gas miner).
            var dT = Thot - Tcold;
            N *= MathF.Tanh(dT/700); // https://www.wolframalpha.com/input?i=tanh(x/700)+from+0+to+1000

            var transfer = Wmax * N;
            electricalEnergy = transfer * component.PowerFactor;
            var outTransfer = transfer * (1 - component.ThermalEfficiency);

            // Adjust thermal energy in transferred gas mixtures.
            if (hotA)
            {
                // A -> B
                airA.Temperature -= transfer / cA;
                airB.Temperature += outTransfer / cB;
            }
            else
            {
                // B -> A
                airA.Temperature += outTransfer / cA;
                airB.Temperature -= transfer / cB;
            }
        }

        component.LastGeneration = electricalEnergy;

        // Turn energy (at atmos tick rate) into wattage.
        var power = electricalEnergy / args.dt;
        // Add ramp factor. This magics slight power into existence, but allows us to ramp up.
        supplier.MaxSupply = power * component.RampFactor;

        var circAComp = Comp<TegCirculatorComponent>(circA);
        var circBComp = Comp<TegCirculatorComponent>(circB);

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

        powerReceiver.PowerDisabled = !group.IsFullyBuilt;

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
            UpdateCirculatorAppearance(uid, false);
        }
    }

    private void UpdateCirculatorAppearance(EntityUid uid, bool powered)
    {
        var circ = Comp<TegCirculatorComponent>(uid);

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

        _appearance.SetData(uid, TegVisuals.CirculatorSpeed, speed);
        _appearance.SetData(uid, TegVisuals.CirculatorPower, powered);

        if (_pointLight.TryGetLight(uid, out var pointLight))
        {
            _pointLight.SetEnabled(uid, powered, pointLight);
            _pointLight.SetColor(uid, speed == TegCirculatorSpeed.SpeedFast ? circ.LightColorFast : circ.LightColorSlow, pointLight);
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

        return new TegSensorData.Circulator(
            inlet.Air.Pressure,
            outlet.Air.Pressure,
            inlet.Air.Temperature,
            outlet.Air.Temperature);
    }
}
