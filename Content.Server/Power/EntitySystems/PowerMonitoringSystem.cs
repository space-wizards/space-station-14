using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;
using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
public sealed class PowerMonitoringSystem : EntitySystem
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

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIOpenedEvent>(OnBoundUiOpen);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, PowerMonitoringSetUIStateEvent>(OnPowerMonitoringConsoleChanged);

        SubscribeLocalEvent<PowerMonitoringDistributorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerMonitoringDistributorComponent, BoundUIOpenedEvent>(OnBoundUiOpen);
        SubscribeLocalEvent<PowerMonitoringDistributorComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<PowerMonitoringDistributorComponent, PowerMonitoringSetUIStateEvent>(OnPowerMonitoringDistributorChanged);
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

    public void UpdateUIState(EntityUid uid,
        PowerMonitoringComponent? powerMonitoring = null,
        NodeContainerComponent? nodeContainer = null,
        PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref powerMonitoring, ref nodeContainer))
            return;

        if (!_nodeContainer.TryGetNode<Node>(nodeContainer, powerMonitoring.LoadNode, out var loadNode))
            return;

        if (!_nodeContainer.TryGetNode<Node>(nodeContainer, powerMonitoring.SourceNode, out var sourceNode))
            return;

        // Get loads and sources
        var totalLoads = GetTotalLoadsForNode(uid, loadNode, out var loads);
        var totalSources = GetTotalSourcesForNode(uid, sourceNode, out var sources);

        // Sort loads and sources
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);

        // Get battery values (if applicable)
        Resolve(uid, ref netBattery, false);
        var external = netBattery != null ? netBattery.NetworkBattery.LastExternalPowerState : ExternalPowerState.None;
        var charge = netBattery != null ? netBattery.NetworkBattery.CurrentStorage / netBattery.NetworkBattery.Capacity : 0f;

        // Raise event to set the new UI state
        var state = new PowerMonitoringBoundInterfaceState(totalSources, totalLoads, sources.ToArray(), loads.ToArray(), charge, external);
        var ev = new PowerMonitoringSetUIStateEvent(uid, state);
        RaiseLocalEvent(uid, ev);
    }

    private double GetTotalSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringEntry> sources)
    {
        var totalSources = 0.0d;
        sources = new List<PowerMonitoringEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalSources;

        foreach (PowerSupplierComponent powerSupplier in netQ.Suppliers)
        {
            if (uid == powerSupplier.Owner)
                continue;

            var supply = powerSupplier.Enabled ? powerSupplier.MaxSupply : 0f;

            sources.Add(LoadOrSource(powerSupplier, supply, false));
            totalSources += supply;
        }

        foreach (BatteryDischargerComponent batteryDischarger in netQ.Dischargers)
        {
            if (uid == batteryDischarger.Owner)
                continue;

            if (!TryComp(batteryDischarger.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentSupply;
            sources.Add(LoadOrSource(batteryDischarger, rate, true));
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

        foreach (PowerConsumerComponent powerConsumer in netQ.Consumers)
        {
            if (uid == powerConsumer.Owner)
                continue;

            if (!powerConsumer.ShowInMonitor)
                continue;

            loads.Add(LoadOrSource(powerConsumer, powerConsumer.DrawRate, false));
            totalLoads += powerConsumer.DrawRate;
        }

        foreach (BatteryChargerComponent batteryCharger in netQ.Chargers)
        {
            if (uid == batteryCharger.Owner)
                continue;

            if (!TryComp(batteryCharger.Owner, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentReceiving;
            loads.Add(LoadOrSource(batteryCharger, rate, true));
            totalLoads += rate;
        }

        return totalLoads;
    }

    public ExternalPowerState CalcExtPowerState(EntityUid uid, PowerState.Battery battery)
    {
        if (MathHelper.CloseTo(battery.CurrentReceiving, 0))
            return ExternalPowerState.None;

        if (!MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 1f) &&
            MathHelper.CloseToPercent(battery.CurrentReceiving, battery.CurrentSupply, 0.05f))
            return ExternalPowerState.Stable;

        if (!MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 1f) &&
            battery.CurrentReceiving - battery.CurrentSupply < 0f)
            return ExternalPowerState.Low;

        return ExternalPowerState.Good;
    }

    private PowerMonitoringEntry LoadOrSource(Component component, double rate, bool isBattery)
    {
        var metaData = MetaData(component.Owner);
        var netEntity = _entityManager.GetNetEntity(component.Owner);
        return new PowerMonitoringEntry(netEntity, metaData.EntityName, rate, isBattery);
    }

    private int CompareLoadOrSources(PowerMonitoringEntry x, PowerMonitoringEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
    private void OnMapInit(EntityUid uid, PowerMonitoringComponent component, MapInitEvent args)
    {
        UpdateUIState(uid, component);
    }

    private void OnBoundUiOpen(EntityUid uid, PowerMonitoringComponent component, BoundUIOpenedEvent args)
    {
        UpdateUIState(uid, component);
    }

    private void OnBatteryChargeChanged(EntityUid uid, PowerMonitoringComponent component, ref ChargeChangedEvent args)
    {
        PowerNetworkBatteryComponent? battery = null;
        if (!Resolve(uid, ref battery, false))
            return;

        var extPowerState = CalcExtPowerState(uid, battery.NetworkBattery);
        if (extPowerState != battery.NetworkBattery.LastExternalPowerState)
        {
            battery.NetworkBattery.LastExternalPowerState = extPowerState;
            UpdateUIState(uid, component);
        }
    }

    private void OnPowerMonitoringConsoleChanged(EntityUid uid, PowerMonitoringConsoleComponent component, PowerMonitoringSetUIStateEvent args)
    {
        if (_userInterfaceSystem.TryGetUi(uid, PowerMonitoringConsoleUiKey.Key, out var bui))
            _userInterfaceSystem.SetUiState(bui, args.State);
    }

    private void OnPowerMonitoringDistributorChanged(EntityUid uid, PowerMonitoringDistributorComponent component, PowerMonitoringSetUIStateEvent args)
    {
        if (_userInterfaceSystem.TryGetUi(uid, PowerMonitoringDistributorUiKey.Key, out var bui))
            _userInterfaceSystem.SetUiState(bui, args.State);
    }
}
