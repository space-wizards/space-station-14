using Content.Server.GameTicking.Rules.Components;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Server.StationEvents.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed partial class PowerMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;

    private List<EntityUid> _trackedDevices = new();
    private bool _powerNetAbnormalities = false;
    private const float RoguePowerConsumerThreshold = 100000;
    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, RequestPowerMonitoringUpdateMessage>(OnUpdateRequestReceived);
        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvsEvent);
        SubscribeLocalEvent<GameRuleStartedEvent>(OnPowerGridCheckStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnPowerGridCheckEnded);
    }

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            // On update, update our list of anchored power monitoring devices
            _trackedDevices.Clear();

            var query = AllEntityQuery<PowerMonitoringDeviceComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var _, out var xform))
            {
                if (xform.Anchored)
                    _trackedDevices.Add(ent);
            }
        }
    }

    // Sends the list of tracked power monitoring devices to all player sessions with one or more power monitoring consoles open
    // This expansion of PVS is needed so that the sprites for these device are available to the the player UI
    // Out-of-range devices will be automatically removed from the player PVS when the UI closes
    private void OnExpandPvsEvent(ref ExpandPvsEvent ev)
    {
        var query = AllEntityQuery<PowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            if (_userInterfaceSystem.SessionHasOpenUi(uid, PowerMonitoringConsoleUiKey.Key, ev.Session))
            {
                if (ev.Entities == null)
                    ev.Entities = new List<EntityUid>();

                foreach (var ent in _trackedDevices)
                    ev.Entities.Add(ent);

                break;
            }
        }
    }

    private void OnUpdateRequestReceived(EntityUid uid, PowerMonitoringConsoleComponent component, RequestPowerMonitoringUpdateMessage args)
    {
        UpdateUIState(uid, component, GetEntity(args.FocusDevice), args.Session);
    }

    private void OnPowerGridCheckStarted(ref GameRuleStartedEvent ev)
    {
        if (HasComp<PowerGridCheckRuleComponent>(ev.RuleEntity))
            _powerNetAbnormalities = true;
    }

    private void OnPowerGridCheckEnded(ref GameRuleEndedEvent ev)
    {
        if (HasComp<PowerGridCheckRuleComponent>(ev.RuleEntity))
            _powerNetAbnormalities = false;
    }

    public void UpdateUIState(EntityUid uid, PowerMonitoringConsoleComponent powerMonitoring, EntityUid? focus, ICommonSession session)
    {
        if (!_userInterfaceSystem.TryGetUi(uid, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = Transform(uid);

        if (consoleXform?.GridUid == null)
            return;

        var gridUid = consoleXform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
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
        var cableNetwork = GetPowerCableNetworkBitMask(gridUid, mapGrid);
        var focusNetwork = new Dictionary<Vector2i, NavMapChunkPowerCables>();
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
        foreach (var ent in _trackedDevices)
        {
            var xform = Transform(ent);
            if (xform.Anchored == false || xform.GridUid != gridUid)
                continue;

            // Generate a new console entry
            if (!TryMakeConsoleEntry(ent, out var entry))
                continue;

            if (entry.Group == PowerMonitoringConsoleGroup.Generator)
            {
                // Covers most power sources
                if (TryComp<PowerSupplierComponent>(ent, out var supplier))
                {
                    entry.PowerValue = supplier.CurrentSupply;
                    totalSources += entry.PowerValue;
                }

                // Radiation collectors
                else if (TryComp<BatteryDischargerComponent>(ent, out var discharger) &&
                    TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                {
                    entry.PowerValue = battery.NetworkBattery.CurrentSupply;
                    totalSources += entry.PowerValue;
                }
            }

            else if (entry.Group == PowerMonitoringConsoleGroup.SMES ||
                entry.Group == PowerMonitoringConsoleGroup.Substation ||
                entry.Group == PowerMonitoringConsoleGroup.APC)
            {

                if (TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                {
                    entry.PowerValue = battery.CurrentSupply;

                    // Load due to network battery recharging
                    totalLoads += Math.Max(battery.CurrentReceiving - battery.CurrentSupply, 0f);

                    // Track battery usage
                    totalBatteryUsage += Math.Max(battery.CurrentSupply - battery.CurrentReceiving, 0f);

                    // Get loads attached to the APC
                    if (entry.Group == PowerMonitoringConsoleGroup.APC && battery.Enabled)
                    {
                        totalLoads += battery.NetworkBattery.LoadingNetworkDemand;
                    }
                }
            }

            allEntries.Add(entry);
        }

        // Get data for the device currently selected on the power monitoring console (if applicable)
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

                var reachableNodes = reachableSourceNodes.Concat(reachableLoadNodes).ToList();
                focusNetwork = GetPowerCableNetworkBitMask(gridUid, mapGrid, reachableNodes);
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
                cableNetwork,
                focusNetwork,
                flags),
            session);
    }

    private void GetSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> sources)
    {
        sources = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return;

        var currentSupply = 0f;
        var currentDemand = 0f;

        foreach (var powerSupplier in netQ.Suppliers)
        {
            var ent = powerSupplier.Owner;

            if (uid == ent)
                continue;

            if (TryMakeConsoleEntry(ent, out var entry, powerSupplier.CurrentSupply))
                sources.Add(entry);

            currentSupply += powerSupplier.CurrentSupply;
        }

        foreach (var batteryDischarger in netQ.Dischargers)
        {
            var ent = batteryDischarger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                continue;

            if (TryMakeConsoleEntry(ent, out var entry, battery.CurrentSupply))
                sources.Add(entry);

            currentSupply += battery.CurrentSupply;
        }

        // Get the total demand for the network
        foreach (var powerConsumer in netQ.Consumers)
        {
            currentDemand += powerConsumer.ReceivedPower;
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                continue;

            currentDemand += battery.CurrentReceiving;
        }

        if (MathHelper.CloseTo(currentDemand, 0))
            return;

        if (MathHelper.CloseTo(currentSupply, 0))
            return;

        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var _battery))
            return;

        // Update the power value for each source based on the fraction of power the entity is actually draining from each
        var powerFraction = Math.Min(_battery.CurrentReceiving / currentSupply, 1f) * Math.Min(currentSupply / currentDemand, 1f);

        foreach (var entry in sources)
        {
            entry.PowerValue *= powerFraction;
        }
    }

    private void GetLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return;

        var currentDemand = 0f;

        foreach (var powerConsumer in netQ.Consumers)
        {
            var ent = powerConsumer.Owner;

            if (uid == ent)
                continue;

            if (TryMakeConsoleEntry(ent, out var entry, powerConsumer.ReceivedPower))
                loads.Add(entry);

            currentDemand += powerConsumer.ReceivedPower;
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                continue;

            if (TryMakeConsoleEntry(ent, out var entry, battery.CurrentReceiving))
                loads.Add(entry);

            currentDemand += battery.CurrentReceiving;
        }

        if (MathHelper.CloseTo(currentDemand, 0))
            return;

        var supplying = 0f;

        if (TryComp<PowerNetworkBatteryComponent>(uid, out var _battery))
            supplying = _battery.CurrentSupply;

        else if (TryComp<PowerSupplierComponent>(uid, out var _supplier))
            supplying = _supplier.CurrentSupply;

        var powerFraction = Math.Min(supplying / currentDemand, 1f);

        foreach (var entry in loads)
        {
            entry.PowerValue *= powerFraction;
        }
    }

    private bool TryMakeConsoleEntry(EntityUid uid, [NotNullWhen(true)] out PowerMonitoringConsoleEntry? entry, double powerValue = 0d)
    {
        entry = null;

        if (!TryComp<PowerMonitoringDeviceComponent>(uid, out var device))
            return false;

        var metaData = MetaData(uid);
        var xform = Transform(uid);
        NetCoordinates? coordinates = device.LocationOnMonitor ? GetNetCoordinates(xform.Coordinates) : null;
        var netEntity = GetNetEntity(uid);

        entry = new PowerMonitoringConsoleEntry(netEntity, coordinates, device.Group, metaData.EntityName, powerValue);
        return true;
    }

    private int AlphabeticalSort(PowerMonitoringConsoleEntry x, PowerMonitoringConsoleEntry y)
    {
        return x.NameLocalized.CompareTo(y.NameLocalized);
    }
}
