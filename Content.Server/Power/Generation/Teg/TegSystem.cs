using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// Handles processing logic for the thermo-electric generator (TEG).
/// </summary>
/// <remarks>
/// <para>
/// The TEG generates power by exchanging heat between gases flowing through its sides.
/// </para>
/// <para>
/// Unlike in SS13, the TEG actually adjusts gas heat exchange to match the energy demand of the power network.
/// To achieve this, the TEG implements its own ramping logic instead of using the built-in
/// </para>
/// </remarks>
/// <seealso cref="TegGeneratorComponent"/>
/// <seealso cref="TegCirculatorComponent"/>
public sealed class TegSystem : EntitySystem
{
    private const string NodeNameTeg = "teg";
    private const string NodeNameInlet = "inlet";
    private const string NodeNameOutlet = "outlet";

    public const string DeviceNetworkCommandSyncData = "teg_sync_data";

    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;

    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TegGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<TegGeneratorComponent, DeviceNetworkPacketEvent>(DeviceNetworkPacketReceived);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, AtmosDeviceUpdateEvent args)
    {
        var circulators = GetBothCirculators(uid);
        if (!circulators.HasValue)
            return;

        var supplier = Comp<PowerSupplierComponent>(uid);

        var (circA, circB) = circulators.Value;

        var (inletA, outletA) = GetPipes(circA);
        var (inletB, outletB) = GetPipes(circB);

        var airA = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var airB = GetCirculatorAirTransfer(inletB.Air, outletB.Air);

        var cA = _atmosphere.GetHeatCapacity(airA);
        var cB = _atmosphere.GetHeatCapacity(airB);

        // Shift ramp position based on demand and generation from previous tick.
        var curRamp = component.RampPosition;
        var lastDraw = supplier.CurrentSupply;
        // Limit amount lost/gained based on power factor.
        curRamp = MathHelper.Clamp(lastDraw, curRamp / component.RampFactor, curRamp * component.RampFactor);
        curRamp = MathF.Max(curRamp, component.RampMinimum);
        component.RampPosition = curRamp;

        var electricalEnergy = 0f;

        if (airA.Pressure > 0 && airB.Pressure > 0)
        {
            // Calculate maximum amount of energy to generate this tick based on ramping above.
            // This clamps the thermal energy transfer as well.
            var targetEnergy = curRamp / _atmosphere.AtmosTickRate;
            var transferMax = targetEnergy / (component.ThermalEfficiency * component.PowerFactor);

            // Calculate thermal and electrical energy transfer between the two sides.
            var deltaT = MathF.Abs(airA.Temperature - airB.Temperature);
            // TODO: account for electrical energy when equalizing.
            var transfer = Math.Min(deltaT * cA * cB / (cA + cB), transferMax);
            electricalEnergy = transfer * component.ThermalEfficiency * component.PowerFactor;
            var outTransfer = transfer * (1 - component.ThermalEfficiency);

            // Adjust thermal energy in transferred gas mixtures.
            if (airA.Temperature > airB.Temperature)
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
        var power = electricalEnergy * _atmosphere.AtmosTickRate;
        // Add ramp factor. This magics slight power into existence, but allows us to ramp up.
        supplier.MaxSupply = power * component.RampFactor;

        _atmosphere.Merge(outletA.Air, airA);
        _atmosphere.Merge(outletB.Air, airB);
    }

    private (EntityUid a, EntityUid b)? GetBothCirculators(EntityUid uidGenerator)
    {
        NodeContainerComponent? nodeContainer = null;
        if (!_nodeContainerQuery.Resolve(uidGenerator, ref nodeContainer))
            return null;

        if (!nodeContainer.Nodes.TryGetValue(NodeNameTeg, out var tegNode) || tegNode.NodeGroup == null)
            return null;

        // TODO: Consistently handle circulator locations.
        // Also no LINQ.
        var circulators = tegNode.NodeGroup.Nodes.OfType<TegNodeCirculator>().ToArray();
        if (circulators.Length != 2)
            return null;

        return (circulators[0].Owner, circulators[1].Owner);
    }

    private static GasMixture GetCirculatorAirTransfer(GasMixture airInlet, GasMixture airOutlet)
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
            return airInlet.Remove(transferMoles);
        }

        return new GasMixture();
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
                var circulators = GetBothCirculators(uid);
                if (!circulators.HasValue)
                    return;

                var (circA, circB) = circulators.Value;

                var payload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = DeviceNetworkCommandSyncData,
                    [DeviceNetworkCommandSyncData] = new TegSensorData
                    {
                        CirculatorA = GetCirculatorSensorData(circA),
                        CirculatorB = GetCirculatorSensorData(circB),
                        LastGeneration = component.LastGeneration,
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
