using Content.Shared.Power;
using Content.Server.NodeContainer;
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

    [Dependency]
    private UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;
            foreach (var component in EntityQuery<PowerMonitoringConsoleComponent>())
            {
                UpdateUIState(component.Owner, component);
            }
        }
    }

    public void UpdateUIState(EntityUid target, PowerMonitoringConsoleComponent? pmcComp = null, NodeContainerComponent? ncComp = null)
    {
        if (!Resolve(target, ref pmcComp))
            return;
        if (!Resolve(target, ref ncComp))
            return;

        var totalSources = 0.0d;
        var totalLoads = 0.0d;
        var sources = new List<PowerMonitoringConsoleEntry>();
        var loads = new List<PowerMonitoringConsoleEntry>();
        PowerMonitoringConsoleEntry LoadOrSource(Component comp, double rate, bool isBattery)
        {
            var md = MetaData(comp.Owner);
            var prototype = md.EntityPrototype?.ID ?? "";
            return new PowerMonitoringConsoleEntry(md.EntityName, prototype, rate, isBattery);
        }
        // Right, so, here's what needs to be considered here.
        var netQ = ncComp.GetNode<Node>("hv").NodeGroup as PowerNet;
        if (netQ != null)
        {
            foreach (PowerConsumerComponent pcc in netQ.Consumers)
            {
                loads.Add(LoadOrSource(pcc, pcc.DrawRate, false));
                totalLoads += pcc.DrawRate;
            }
            foreach (BatteryChargerComponent pcc in netQ.Chargers)
            {
                if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                {
                    continue;
                }
                var rate = batteryComp.NetworkBattery.CurrentReceiving;
                loads.Add(LoadOrSource(pcc, rate, true));
                totalLoads += rate;
            }
            foreach (PowerSupplierComponent pcc in netQ.Suppliers)
            {
                sources.Add(LoadOrSource(pcc, pcc.MaxSupply, false));
                totalSources += pcc.MaxSupply;
            }
            foreach (BatteryDischargerComponent pcc in netQ.Dischargers)
            {
                if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                {
                    continue;
                }
                var rate = batteryComp.NetworkBattery.CurrentSupply;
                sources.Add(LoadOrSource(pcc, rate, true));
                totalSources += rate;
            }
        }
        // Sort
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);
        // Actually set state.
        var state = new PowerMonitoringConsoleBoundInterfaceState(totalSources, totalLoads, sources.ToArray(), loads.ToArray());
        _userInterfaceSystem.GetUiOrNull(target, PowerMonitoringConsoleUiKey.Key)?.SetState(state);
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}
