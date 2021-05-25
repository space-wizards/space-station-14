using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Server.UserInterface;
using Content.Server.WireHacking;
using JetBrains.Annotations;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class PowerMonitoringConsoleSystem : EntitySystem
{
    private float _updateTimer = 0.0f;

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= 1)
        {
            _updateTimer -= 1;
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
        PowerMonitoringConsoleEntry LoadOrSource(Component comp, double rate)
        {
            var md = MetaData(comp.Owner);
            var prototype = md.EntityPrototype?.ID ?? "";
            return new PowerMonitoringConsoleEntry(md.EntityName, prototype, rate);
        }
        // Right, so, here's what needs to be considered here.
        var netQ = ncComp.GetNode<Node>("hv").NodeGroup as PowerNet;
        if (netQ != null)
        {
            var net = netQ!;
            foreach (PowerConsumerComponent pcc in net.Consumers)
            {
                if (pcc.DrawRate <= 0)
                {
                    // Grilles & other inactive power consumers.
                    continue;
                }
                loads.Add(LoadOrSource(pcc, pcc.DrawRate));
                totalLoads += pcc.DrawRate;
            }
            foreach (BatteryChargerComponent pcc in net.Chargers)
            {
                if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                {
                    continue;
                }
                var rate = batteryComp.NetworkBattery.CurrentReceiving;
                loads.Add(LoadOrSource(pcc, rate));
                totalLoads += rate;
            }
            foreach (PowerSupplierComponent pcc in net.Suppliers)
            {
                sources.Add(LoadOrSource(pcc, pcc.MaxSupply));
                totalSources += pcc.MaxSupply;
            }
            foreach (BatteryDischargerComponent pcc in net.Dischargers)
            {
                if (!TryComp(pcc.Owner, out PowerNetworkBatteryComponent? batteryComp))
                {
                    continue;
                }
                var rate = batteryComp.NetworkBattery.CurrentSupply;
                sources.Add(LoadOrSource(pcc, rate));
                totalSources += rate;
            }
        }
        // Sort
        loads.Sort(CompareLoadOrSources);
        sources.Sort(CompareLoadOrSources);
        // Actually set state.
        target.GetUIOrNull(PowerMonitoringConsoleUiKey.Key)?.SetState(new PowerMonitoringConsoleBoundInterfaceState(totalSources, totalLoads, sources.ToArray(), loads.ToArray()));
    }

    private int CompareLoadOrSources(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return -x.Size.CompareTo(y.Size);
    }
}

