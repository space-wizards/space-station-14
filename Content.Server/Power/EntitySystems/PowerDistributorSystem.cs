using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Server.Power.Pow3r;
using Content.Shared.APC;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerDistributorSystem : EntitySystem
{
    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<PowerDistributorComponent, BoundUIOpenedEvent>(OnBoundUiOpen);
        SubscribeLocalEvent<PowerDistributorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerDistributorComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
    }

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = EntityQueryEnumerator<PowerDistributorComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                UpdateUIState(uid, component);
            }
        }
    }

    public void UpdateUIState(EntityUid target, PowerDistributorComponent? pmcComp = null, NodeContainerComponent? ncComp = null, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(target, ref pmcComp, ref ncComp))
            return;

        if (!Resolve(target, ref netBattery))
            return;

        // Right, so, here's what needs to be considered here.
        if (!_nodeContainer.TryGetNode<Node>(ncComp, pmcComp.LoadNode, out var loadNode))
            return;

        if (!_nodeContainer.TryGetNode<Node>(ncComp, pmcComp.SourceNode, out var sourceNode))
            return;

        var totalLoads = GetTotalLoadsForNode(target, loadNode, out var loads);
        var totalSources = GetTotalSourcesForNode(target, sourceNode, out var sources);

        // Sort
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);

        var battery = netBattery.NetworkBattery;
        var power = (int) MathF.Ceiling(battery.CurrentSupply);
        var ext = pmcComp.LastExternalState;
        var charge = battery.CurrentStorage / battery.Capacity;

        ///
        var extPowerState = CalcExtPowerState(target, battery);
        if (extPowerState != pmcComp.LastExternalState)
        {
            pmcComp.LastExternalState = extPowerState;
            UpdateUIState(target, pmcComp);
        }
        ///

        // Actually set state.
        if (_userInterfaceSystem.TryGetUi(target, PowerDistributorUiKey.Key, out var bui))
            _userInterfaceSystem.SetUiState(bui, new PowerDistributorBoundInterfaceState(power, ext, charge, totalSources, totalLoads, sources.ToArray(), loads.ToArray()));
    }

    private double GetTotalSourcesForNode(EntityUid uid, Node node, out List<PowerDistributorEntry> sources)
    {
        var totalSources = 0.0d;
        sources = new List<PowerDistributorEntry>();

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

    private double GetTotalLoadsForNode(EntityUid uid, Node node, out List<PowerDistributorEntry> loads)
    {
        var totalLoads = 0.0d;
        loads = new List<PowerDistributorEntry>();

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

    private PowerDistributorEntry LoadOrSource(Component comp, double rate, bool isBattery)
    {
        var md = MetaData(comp.Owner);
        var prototype = md.EntityPrototype?.ID ?? "";
        return new PowerDistributorEntry(md.EntityName, prototype, rate, isBattery);
    }

    private int CompareLoadOrSources(PowerDistributorEntry x, PowerDistributorEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }

    // Change the APC's state only when the battery state changes, or when it's first created.
    private void OnBatteryChargeChanged(EntityUid uid, PowerDistributorComponent component, ref ChargeChangedEvent args)
    {
        UpdatePowerDistributorState(uid, component);
    }

    private void OnMapInit(EntityUid uid, PowerDistributorComponent component, MapInitEvent args)
    {
        UpdatePowerDistributorState(uid, component);
    }

    //Update the HasAccess var for UI to read
    private void OnBoundUiOpen(EntityUid uid, PowerDistributorComponent component, BoundUIOpenedEvent args)
    {
        UpdatePowerDistributorState(uid, component);
    }

    public void UpdatePowerDistributorState(EntityUid uid,
        PowerDistributorComponent? pdc = null,
        PowerNetworkBatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref pdc, ref battery, false))
            return;

        var extPowerState = CalcExtPowerState(uid, battery.NetworkBattery);
        if (extPowerState != pdc.LastExternalState)
        {
            pdc.LastExternalState = extPowerState;
            UpdateUIState(uid, pdc);
        }
    }

    private PowerDistributorExternalPowerState CalcExtPowerState(EntityUid uid, PowerState.Battery battery)
    {
        if (battery.CurrentReceiving == 0 && !MathHelper.CloseTo(battery.CurrentStorage / battery.Capacity, 1))
        {
            return PowerDistributorExternalPowerState.None;
        }

        var delta = battery.CurrentSupply - battery.CurrentReceiving;
        if (!MathHelper.CloseToPercent(delta, 0, 0.1f) && delta < 0)
        {
            return PowerDistributorExternalPowerState.Low;
        }

        return PowerDistributorExternalPowerState.Good;
    }
}
