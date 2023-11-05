using Content.Server.GameTicking.Rules.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Server.Power.NodeGroups;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed partial class PowerMonitoringConsoleSystem : SharedPowerMonitoringConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;

    private Dictionary<EntityUid, List<(EntityUid, PowerMonitoringDeviceComponent)>> _trackedDevices = new();
    private bool _powerNetAbnormalities = false;

    private const float RoguePowerConsumerThreshold = 100000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvsEvent);
        SubscribeLocalEvent<GameRuleStartedEvent>(OnPowerGridCheckStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnPowerGridCheckEnded);

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, RequestPowerMonitoringUpdateMessage>(OnUpdateRequestReceived);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, GridSplitEvent>(OnGridSplit);

        SubscribeLocalEvent<CableComponent, CableAnchorStateChangedEvent>(OnCableAnchorStateChanged);
        SubscribeLocalEvent<PowerMonitoringDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchoringChanged);
        SubscribeLocalEvent<PowerMonitoringDeviceComponent, NodeGroupsRebuilt>(OnNodeGroupRebuilt);

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIClosedEvent>(OnUIClosed);
    }

    public void UpdateUIState
        (EntityUid uid,
        PowerMonitoringConsoleComponent powerMonitoringConsole,
        EntityUid? focus,
        PowerMonitoringConsoleGroup? focusGroup,
        ICommonSession session)
    {
        // Ensure that all entities that are to be joined together have been assigned an exemplar
        //UpdateEntityExamplers();

        if (!_userInterfaceSystem.TryGetUi(uid, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = Transform(uid);

        if (consoleXform?.GridUid == null)
            return;

        var gridUid = consoleXform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        if (!_trackedDevices.TryGetValue(gridUid, out var gridDevices))
            return;

        // The grid must have a NavMapComponent to visualize the map 
        EnsureComp<NavMapComponent>(gridUid);

        // Data to be send to the client
        var totalSources = 0d;
        var totalBatteryUsage = 0d;
        var totalLoads = 0d;
        var allEntries = new List<PowerMonitoringConsoleEntry>();
        var sourcesForFocus = new List<PowerMonitoringConsoleEntry>();
        var loadsForFocus = new List<PowerMonitoringConsoleEntry>();
        var flags = PowerMonitoringFlags.None;

        // Power grid anomalies event is in-progress
        if (_powerNetAbnormalities)
            flags |= PowerMonitoringFlags.PowerNetAbnormalities;

        // Power consumers 
        var powerConsumerQuery = AllEntityQuery<PowerConsumerComponent>();
        while (powerConsumerQuery.MoveNext(out var ent, out var powerConsumer))
        {
            if (powerConsumer.ReceivedPower >= RoguePowerConsumerThreshold)
                flags |= PowerMonitoringFlags.RoguePowerConsumer;

            totalLoads += powerConsumer.DrawRate;
        }

        // Tracked devices
        foreach ((var ent, var device) in gridDevices)
        {
            // Ignore joined, non-exemplar entities
            if (device.JoinAlikeEntities && !device.IsExemplar)
                continue;

            // Ignore unachored devices or those on another grid
            var xform = Transform(ent);
            if (xform.Anchored == false || xform.GridUid != gridUid)
                continue;

            // Generate a new console entry
            var entry = new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent));
            entry.PowerValue = GetPrimaryPowerValues(ent, device, out var powerSupplied, out var powerUsage, out var batteryUsage);

            // Update running totals
            totalSources += powerSupplied;
            totalLoads += powerUsage;
            totalBatteryUsage += batteryUsage;

            // Only add the entry if the device is in the current focus group
            if (focusGroup != null && device.Group == focusGroup)
                allEntries.Add(entry);
        }

        // Get data for the device currently selected on the power monitoring console (if applicable)
        if (powerMonitoringConsole.Focus != focus)
        {
            powerMonitoringConsole.LastReachableNodes = null;
            powerMonitoringConsole.FocusChunks.Clear();
            powerMonitoringConsole.Focus = focus;
            Dirty(uid, powerMonitoringConsole);
        }

        if (focus != null)
        {
            if (TryComp<NodeContainerComponent>(focus, out var nodeContainer) &&
                TryComp<PowerMonitoringDeviceComponent>(focus, out var device))
            {
                List<Node> reachableSourceNodes = new List<Node>();
                List<Node> reachableLoadNodes = new List<Node>();

                if (nodeContainer.Nodes.TryGetValue(device.SourceNode, out var sourceNode))
                {
                    GetSourcesForNode(focus.Value, sourceNode, out sourcesForFocus);
                    reachableSourceNodes = FloodFillNode(sourceNode);
                }

                var loadNodeName = device.LoadNode;

                // Search for the enabled load node (required for portable generators)
                if (device.LoadNodes != null)
                {
                    var foundNode = nodeContainer.Nodes.FirstOrNull(x => x.Value is CableDeviceNode && (x.Value as CableDeviceNode)?.Enabled == true);

                    if (foundNode != null)
                        loadNodeName = foundNode.Value.Key;
                }

                if (nodeContainer.Nodes.TryGetValue(loadNodeName, out var loadNode))
                {
                    GetLoadsForNode(focus.Value, loadNode, out loadsForFocus);
                    reachableLoadNodes = FloodFillNode(loadNode);
                }

                var reachableNodes = reachableSourceNodes.Concat(reachableLoadNodes).Select(x => x.Owner).ToList();

                // Quick check to see if we need to update the focus power cable network
                // If the number of nodes, or the first or last node, changes then we probably need to update
                if (powerMonitoringConsole.LastReachableNodes == null ||
                    powerMonitoringConsole.LastReachableNodes.Count != reachableNodes.Count ||
                    powerMonitoringConsole.LastReachableNodes.First() != reachableNodes.First() ||
                    powerMonitoringConsole.LastReachableNodes.Last() != reachableNodes.Last())
                {
                    powerMonitoringConsole.LastReachableNodes = reachableNodes;
                    UpdateFocusNetwork(uid, powerMonitoringConsole, gridUid, mapGrid, reachableNodes);
                }
            }
        }

        // Sort found devices alphabetically (not by power usage; otherwise their position on the UI will shift)
        allEntries.Sort(AlphabeticalSort);
        sourcesForFocus.Sort(AlphabeticalSort);
        loadsForFocus.Sort(AlphabeticalSort);

        // Set the UI state
        _userInterfaceSystem.SetUiState(bui,
            new PowerMonitoringConsoleBoundInterfaceState
                (totalSources,
                totalBatteryUsage,
                totalLoads,
                allEntries.ToArray(),
                sourcesForFocus.ToArray(),
                loadsForFocus.ToArray(),
                flags),
            session);
    }

    private double GetPrimaryPowerValues(EntityUid uid, PowerMonitoringDeviceComponent device, out double powerSupplied, out double powerUsage, out double batteryUsage)
    {
        var powerValue = 0d;
        powerSupplied = 0d;
        powerUsage = 0d;
        batteryUsage = 0d;

        if (device.Group == PowerMonitoringConsoleGroup.Generator)
        {
            // Covers most power sources
            if (TryComp<PowerSupplierComponent>(uid, out var supplier))
            {
                powerValue = supplier.CurrentSupply;
                powerSupplied += powerValue;
            }

            // Radiation collectors
            else if (TryComp<BatteryDischargerComponent>(uid, out var _) &&
                TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            {
                powerValue = battery.NetworkBattery.CurrentSupply;
                powerSupplied += powerValue;
            }
        }

        else if (device.Group == PowerMonitoringConsoleGroup.SMES ||
            device.Group == PowerMonitoringConsoleGroup.Substation ||
            device.Group == PowerMonitoringConsoleGroup.APC)
        {

            if (TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            {
                powerValue = battery.CurrentSupply;

                // Load due to network battery recharging
                powerUsage += Math.Max(battery.CurrentReceiving - battery.CurrentSupply, 0d);

                // Track battery usage
                batteryUsage += Math.Max(battery.CurrentSupply - battery.CurrentReceiving, 0d);

                // Get loads attached to the APC
                if (device.Group == PowerMonitoringConsoleGroup.APC && battery.Enabled)
                {
                    powerUsage += battery.NetworkBattery.LoadingNetworkDemand;
                }
            }
        }

        // Update power values with those from any child entities, if applicable
        if (device.JoinAlikeEntities && device.IsExemplar)
        {
            foreach (var child in device.ChildEntities)
            {
                if (child == uid)
                    continue;

                if (!TryComp<PowerMonitoringDeviceComponent>(child, out var childDevice) ||
                    (childDevice.IsExemplar && childDevice.ChildEntities.Contains(uid)))
                    continue;

                var childPowerValue = GetPrimaryPowerValues(child, childDevice, out var childPowerSupplied, out var childPowerUsage, out var childBatteryUsage);

                powerValue += childPowerValue;
                powerSupplied += childPowerSupplied;
                powerUsage += childPowerUsage;
                batteryUsage += childBatteryUsage;
            }
        }

        return powerValue;
    }

    private void GetSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> sources)
    {
        sources = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return;

        var indexedSources = new Dictionary<EntityUid, PowerMonitoringConsoleEntry>();
        var currentSupply = 0f;
        var currentDemand = 0f;

        foreach (var powerSupplier in netQ.Suppliers)
        {
            var ent = powerSupplier.Owner;

            if (uid == ent)
                continue;

            currentSupply += powerSupplier.CurrentSupply;

            if (TryComp<PowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Groups entities with the same prototype into one entry
                if (entDevice.JoinAlikeEntities && !entDevice.IsExemplar)
                    ent = entDevice.ExemplarUid;

                if (indexedSources.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += powerSupplier.CurrentSupply;
                    indexedSources[ent] = entry;

                    continue;
                }

                indexedSources.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), powerSupplier.CurrentSupply));
            }
        }

        foreach (var batteryDischarger in netQ.Dischargers)
        {
            var ent = batteryDischarger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var entBattery))
                continue;

            currentSupply += entBattery.CurrentSupply;

            if (TryComp<PowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Groups entities with the same prototype into one entry
                if (entDevice.JoinAlikeEntities && !entDevice.IsExemplar)
                    ent = entDevice.ExemplarUid;

                if (indexedSources.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += entBattery.CurrentSupply;
                    indexedSources[ent] = entry;

                    continue;
                }

                indexedSources.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), entBattery.CurrentSupply));
            }
        }

        sources = indexedSources.Values.ToList();

        // Get the total demand for the network
        foreach (var powerConsumer in netQ.Consumers)
        {
            currentDemand += powerConsumer.ReceivedPower;
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var entBattery))
                continue;

            currentDemand += entBattery.CurrentReceiving;
        }

        if (MathHelper.CloseTo(currentDemand, 0))
            return;

        if (MathHelper.CloseTo(currentSupply, 0))
            return;

        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            return;

        var powerUsage = battery.CurrentReceiving;

        if (TryComp<PowerMonitoringDeviceComponent>(uid, out var device) && device.IsExemplar)
        {
            foreach (var child in device.ChildEntities)
            {
                if (TryComp<PowerNetworkBatteryComponent>(uid, out var childBattery))
                    powerUsage += childBattery.CurrentReceiving;
            }
        }

        // Update the power value for each source based on the fraction of power the entity is actually draining from each
        var powerFraction = Math.Min(powerUsage / currentSupply, 1f) * Math.Min(currentSupply / currentDemand, 1f);

        foreach (var entry in sources)
            entry.PowerValue *= powerFraction;
    }

    private void GetLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return;

        var indexedLoads = new Dictionary<EntityUid, PowerMonitoringConsoleEntry>();
        var currentDemand = 0f;

        foreach (var powerConsumer in netQ.Consumers)
        {
            var ent = powerConsumer.Owner;

            if (uid == ent)
                continue;

            currentDemand += powerConsumer.ReceivedPower;

            if (TryComp<PowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Groups entities with the same prototype into one entry
                if (entDevice.JoinAlikeEntities && !entDevice.IsExemplar)
                    ent = entDevice.ExemplarUid;

                if (indexedLoads.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += powerConsumer.ReceivedPower;
                    indexedLoads[ent] = entry;

                    continue;
                }

                indexedLoads.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), powerConsumer.ReceivedPower));
            }
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                continue;

            currentDemand += battery.CurrentReceiving;

            if (TryComp<PowerMonitoringDeviceComponent>(ent, out var entDevice))
            {
                // Groups entities with the same prototype into one entry
                if (entDevice.JoinAlikeEntities && !entDevice.IsExemplar)
                    ent = entDevice.ExemplarUid;

                if (indexedLoads.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += battery.CurrentReceiving;
                    indexedLoads[ent] = entry;

                    continue;
                }

                indexedLoads.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), battery.CurrentReceiving));
            }
        }

        loads = indexedLoads.Values.ToList();

        if (MathHelper.CloseTo(currentDemand, 0))
            return;

        var supplying = 0f;

        if (TryComp<PowerNetworkBatteryComponent>(uid, out var entBattery))
            supplying = entBattery.CurrentSupply;

        else if (TryComp<PowerSupplierComponent>(uid, out var entSupplier))
            supplying = entSupplier.CurrentSupply;

        if (TryComp<PowerMonitoringDeviceComponent>(uid, out var device) && device.IsExemplar)
        {
            foreach (var child in device.ChildEntities)
            {
                if (TryComp<PowerNetworkBatteryComponent>(uid, out var childBattery))
                    supplying += childBattery.CurrentSupply;

                else if (TryComp<PowerSupplierComponent>(uid, out var childSupplier))
                    supplying += childSupplier.CurrentSupply;
            }
        }

        var powerFraction = Math.Min(supplying / currentDemand, 1f);

        foreach (var entry in loads)
            entry.PowerValue *= powerFraction;
    }

    private bool TryGetEntProtoId(EntityUid uid, [NotNullWhen(true)] out EntProtoId? entProtoId)
    {
        entProtoId = null;
        var protoId = MetaData(uid)?.EntityPrototype?.ID;

        if (protoId == null)
            return false;

        entProtoId = (EntProtoId) protoId;
        return true;
    }

    private int AlphabeticalSort(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        var nameX = MetaData(EntityManager.GetEntity(x.NetEntity)).EntityName;
        var nameY = MetaData(EntityManager.GetEntity(y.NetEntity)).EntityName;

        return nameX.CompareTo(nameY);
    }
}
