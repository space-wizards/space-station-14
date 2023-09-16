using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerMonitoringConsoleSystem : EntitySystem
{
    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            var query = EntityQueryEnumerator<PowerMonitoringConsoleComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                UpdateUIState(uid, component);
            }
        }
    }

    public void UpdateUIState(EntityUid target, PowerMonitoringConsoleComponent? pmcComp = null, NodeContainerComponent? ncComp = null)
    {
        if (!Resolve(target, ref pmcComp))
            return;

        if (!Resolve(target, ref ncComp))
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

        // Actually set state.
        if (_userInterfaceSystem.TryGetUi(target, PowerMonitoringConsoleUiKey.Key, out var bui))
            _userInterfaceSystem.SetUiState(bui, new PowerMonitoringConsoleBoundInterfaceState(totalSources, totalLoads, sources.ToArray(), loads.ToArray()));
    }

    private double GetTotalSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> sources)
    {
        var totalSources = 0.0d;
        sources = new List<PowerMonitoringConsoleEntry>();

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

    private double GetTotalLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        var totalLoads = 0.0d;
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is ApcNet _)
            return GetTotalLoadsForApcNode(uid, node, out loads);

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

    private double GetTotalLoadsForApcNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        var totalLoads = 0.0d;
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not ApcNet netQ)
            return totalLoads;

        foreach (ApcPowerReceiverComponent pcc in netQ.AllReceivers)
        {
            if (uid == pcc.Owner)
                continue;

            loads.Add(LoadOrSource(pcc, pcc.Load, false));
            totalLoads += pcc.Load;
        }

        return totalLoads;
    }

    private PowerMonitoringConsoleEntry LoadOrSource(Component comp, double rate, bool isBattery)
    {
        var md = MetaData(comp.Owner);
        var prototype = md.EntityPrototype?.ID ?? "";
        return new PowerMonitoringConsoleEntry(md.EntityName, prototype, rate, isBattery);
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}
