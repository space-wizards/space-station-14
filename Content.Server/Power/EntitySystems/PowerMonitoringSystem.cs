using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;
using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.Power.Systems;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
public sealed class PowerMonitoringSystem : SharedPowerMonitoringSystem
{
    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent((ComponentEventHandler<PowerMonitoringComponent, BoundUIOpenedEvent>) OnBoundUiOpen);
        SubscribeLocalEvent((ComponentEventHandler<PowerMonitoringComponent, MapInitEvent>) OnMapInit);
        SubscribeLocalEvent((ComponentEventRefHandler<PowerMonitoringComponent, ChargeChangedEvent>) OnBatteryChargeChanged);
    }

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = EntityQueryEnumerator<PowerMonitoringComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                UpdateUIState(uid, component);
            }
        }
    }

    public void UpdateUIState(EntityUid uid, PowerMonitoringComponent? pdc = null, NodeContainerComponent? ncComp = null, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref pdc, ref ncComp))
            return;

        if (!Resolve(uid, ref netBattery, false))
            return;

        if (!_nodeContainer.TryGetNode<Node>(ncComp, pdc.LoadNode, out var loadNode))
            return;

        if (!_nodeContainer.TryGetNode<Node>(ncComp, pdc.SourceNode, out var sourceNode))
            return;

        var totalLoads = GetTotalLoadsForNode(uid, loadNode, out var loads);
        var totalSources = GetTotalSourcesForNode(uid, sourceNode, out var sources);

        // Sort loads and sources
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);

        var battery = netBattery.NetworkBattery;
        var power = (int) MathF.Ceiling(battery.CurrentReceiving);
        var ext = netBattery.LastExternalPowerState;
        var charge = battery.CurrentStorage / battery.Capacity;

        // Actually set state
        if (_userInterfaceSystem.TryGetUi(uid, PowerMonitoringDistributorUiKey.Key, out var bui))
            _userInterfaceSystem.SetUiState(bui, new PowerMonitoringBoundInterfaceState(power, ext, charge, totalSources, totalLoads, sources.ToArray(), loads.ToArray()));
    }

    private double GetTotalSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringEntry> sources)
    {
        var totalSources = 0.0d;
        sources = new List<PowerMonitoringEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalSources;

        foreach (PowerSupplierComponent pcc in netQ.Suppliers)
        {
            if (uid == pcc.Owner)
                continue;

            var supply = pcc.Enabled
                ? pcc.MaxSupply
                : 0f;

            sources.Add(LoadOrSource(pcc, supply, false));
            totalSources += supply;
        }

        foreach (BatteryDischargerComponent pcc in netQ.Dischargers)
        {
            if (uid == pcc.Owner)
                continue;

            if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentSupply;
            sources.Add(LoadOrSource(pcc, rate, true));
            totalSources += rate;
        }

        return totalSources;
    }

    private double GetTotalLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringEntry> loads)
    {
        var totalLoads = 0.0d;
        loads = new List<PowerMonitoringEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalLoads;

        foreach (PowerConsumerComponent pcc in netQ.Consumers)
        {
            if (uid == pcc.Owner)
                continue;

            if (!pcc.ShowInMonitor)
                continue;

            loads.Add(LoadOrSource(pcc, pcc.DrawRate, false));
            totalLoads += pcc.DrawRate;
        }

        foreach (BatteryChargerComponent pcc in netQ.Chargers)
        {
            if (uid == pcc.Owner)
                continue;

            if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentReceiving;
            loads.Add(LoadOrSource(pcc, rate, true));
            totalLoads += rate;
        }

        return totalLoads;
    }

    private PowerMonitoringEntry LoadOrSource(Component comp, double rate, bool isBattery)
    {
        var md = MetaData(comp.Owner);
        var prototype = md.EntityPrototype?.ID ?? "";
        var netEntity = _entityManager.GetNetEntity(comp.Owner);
        return new PowerMonitoringEntry(netEntity, md.EntityName, prototype, rate, isBattery);
    }

    private int CompareLoadOrSources(PowerMonitoringEntry x, PowerMonitoringEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }

    private void OnBatteryChargeChanged(EntityUid uid, PowerMonitoringComponent component, ref ChargeChangedEvent args)
    {
        UpdatePowerDistributorState(uid, component);
    }

    private void OnMapInit(EntityUid uid, PowerMonitoringComponent component, MapInitEvent args)
    {
        UpdatePowerDistributorState(uid, component);
    }

    private void OnBoundUiOpen(EntityUid uid, PowerMonitoringComponent component, BoundUIOpenedEvent args)
    {
        UpdatePowerDistributorState(uid, component);
    }

    public void UpdatePowerDistributorState(EntityUid uid,
        PowerMonitoringComponent? powerMonitoring = null,
        PowerNetworkBatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref powerMonitoring, ref battery, false))
            return;

        var extPowerState = CalcExtPowerState(uid, battery.NetworkBattery);
        if (extPowerState != battery.NetworkBattery.LastExternalPowerState)
        {
            battery.NetworkBattery.LastExternalPowerState = extPowerState;
            UpdateUIState(uid, powerMonitoring);
        }
    }

    private ExternalPowerState CalcExtPowerState(EntityUid uid, PowerState.Battery battery)
    {
        if (MathHelper.CloseTo(battery.CurrentReceiving, 0))
            return ExternalPowerState.None;

        if (MathHelper.CloseToPercent(battery.CurrentReceiving, battery.CurrentSupply, 0.05f))
            return ExternalPowerState.Stable;

        if (battery.CurrentReceiving - battery.CurrentSupply < 0f)
            return ExternalPowerState.Low;

        return ExternalPowerState.Good;
    }
}
