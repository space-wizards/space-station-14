using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Power.Generation.Teg;

public sealed class TegSystem : EntitySystem
{
    private const string NodeNameTeg = "teg";
    private const string NodeNameInlet = "inlet";
    private const string NodeNameOutlet = "outlet";

    public const string DeviceNetworkCommandSyncData = "teg_sync_data";

    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TegGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<TegGeneratorComponent, DeviceNetworkPacketEvent>(DeviceNetworkPacketReceived);

        _xformQuery = GetEntityQuery<TransformComponent>();
        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, AtmosDeviceUpdateEvent args)
    {
        var circulators = GetBothCirculators(uid);
        if (!circulators.HasValue)
            return;

        var battery = Comp<BatteryComponent>(uid);

        var (circA, circB) = circulators.Value;

        var (inletA, outletA) = GetPipes(circA);
        var (inletB, outletB) = GetPipes(circB);

        var airA = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var airB = GetCirculatorAirTransfer(inletB.Air, outletB.Air);

        var cA = _atmosphere.GetHeatCapacity(airA);
        var cB = _atmosphere.GetHeatCapacity(airB);

        if (airA.Pressure > 0 && airB.Pressure > 0)
        {
            // Clamp energy transfer to battery capacity.
            var batteryAvailable = battery.MaxCharge - battery.CurrentCharge;
            var transferMax = batteryAvailable / (component.ThermalEfficiency * component.PowerFactor);

            var deltaT = MathF.Abs(airA.Temperature - airB.Temperature);
            var transfer = Math.Min(deltaT * cA * cB / (cA + cB), transferMax);
            var electricalEnergy = transfer * component.ThermalEfficiency * component.PowerFactor;
            var realTransfer = transfer * (1 - component.ThermalEfficiency);

            if (airA.Temperature > airB.Temperature)
            {
                // A -> B
                airA.Temperature -= transfer / cA;
                airB.Temperature += realTransfer / cB;
            }
            else
            {
                // B -> A
                airA.Temperature += realTransfer / cA;
                airB.Temperature -= transfer / cB;
            }

            _battery.SetCharge(uid, battery.Charge + electricalEnergy, battery);
            component.LastGeneration = electricalEnergy;
        }

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
                        LastGeneration = component.LastGeneration
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
