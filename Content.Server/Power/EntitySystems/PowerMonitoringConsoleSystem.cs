using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer;
using Content.Server.Power.Components;
using Content.Server.Power.NodeGroups;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Players;
using System.Linq;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed partial class PowerMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;

    private List<EntityUid> _trackedDevices = new();
    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, RequestPowerMonitoringUpdateMessage>(OnUpdateRequestReceived);
        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvsEvent);
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

        // Data to be send to the client
        var totalSources = 0f;
        var totalLoads = 0f;
        var allSources = new List<PowerMonitoringConsoleEntry>();
        var allLoads = new List<PowerMonitoringConsoleEntry>();
        var sourcesForFocus = new List<PowerMonitoringConsoleEntry>();
        var loadsForFocus = new List<PowerMonitoringConsoleEntry>();
        var cableNetwork = GetPowerCableNetworkBitMask(gridUid, mapGrid);
        var focusNetwork = new Dictionary<Vector2i, NavMapChunkPowerCables>();

        foreach (var ent in _trackedDevices)
        {
            // Generate a new console entry
            var powerValue = 0f;

            if (!TryMakeConsoleEntry(ent, out var entry))
                continue;

            var device = Comp<PowerMonitoringDeviceComponent>(ent);

            // Generators
            if (device.Group == PowerMonitoringConsoleGroup.Generator)
            {
                if (TryComp<PowerSupplierComponent>(ent, out var powerSupplier))
                    entry.PowerValue = powerSupplier.MaxSupply;

                else if (TryComp<BatteryDischargerComponent>(ent, out var batteryDischarger) &&
                    TryComp(ent, out PowerNetworkBatteryComponent? battery))
                    entry.PowerValue = battery.NetworkBattery.CurrentSupply;

                allSources.Add(entry);
                totalSources += powerValue;
            }

            // SMES / substation / APC
            else if (device.Group == PowerMonitoringConsoleGroup.SMES ||
                device.Group == PowerMonitoringConsoleGroup.Substation ||
                device.Group == PowerMonitoringConsoleGroup.APC)
            {
                if (TryComp<PowerNetworkBatteryComponent>(ent, out var battery))
                    entry.PowerValue = battery.NetworkBattery.CurrentReceiving;

                allLoads.Add(entry);
                totalLoads += powerValue;
            }

            // Power consumers
            else if (device.Group == PowerMonitoringConsoleGroup.Consumer)
            {
                if (TryComp<PowerConsumerComponent>(ent, out var consumer))
                    entry.PowerValue = consumer.ReceivedPower;

                allLoads.Add(entry);
                totalLoads += powerValue;
            }
        }

        // Get necessary data on the currently selected device on the power monitoring console (if applicable)
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

                if (nodeContainer.Nodes.TryGetValue(device.LoadNode, out var loadNode))
                {
                    GetLoadsForNode(focus.Value, loadNode, out loadsForFocus);
                    reachableLoadNodes = FloodFillNode(loadNode);
                }

                var reachableNodes = reachableSourceNodes.Concat(reachableLoadNodes).ToList();
                focusNetwork = GetPowerCableNetworkBitMask(gridUid, mapGrid, reachableNodes);
            }
        }

        // Sort found devices alphabetically (not by power usage; otherwise their position on the UI will shift)
        allSources.Sort(AlphabeticalSort);
        allLoads.Sort(AlphabeticalSort);

        // Set the UI state
        _userInterfaceSystem.SetUiState(bui,
            new PowerMonitoringConsoleBoundInterfaceState
                (totalSources,
                totalLoads,
                allSources.ToArray(),
                allLoads.ToArray(),
                sourcesForFocus.ToArray(),
                loadsForFocus.ToArray(),
                cableNetwork,
                focusNetwork),
            session);
    }

    private double GetSourcesForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> sources)
    {
        var totalSources = 0.0d;
        sources = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalSources;

        foreach (PowerSupplierComponent powerSupplier in netQ.Suppliers)
        {
            var ent = powerSupplier.Owner;

            if (uid == ent)
                continue;

            var supply = powerSupplier.Enabled ? powerSupplier.MaxSupply : 0f;

            if (TryMakeConsoleEntry(ent, out var entry, supply))
                sources.Add(entry);

            totalSources += supply;
        }

        foreach (var batteryDischarger in netQ.Dischargers)
        {
            var ent = batteryDischarger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp(ent, out PowerNetworkBatteryComponent? battery))
                continue;

            var rate = battery.NetworkBattery.CurrentSupply;

            if (TryMakeConsoleEntry(ent, out var entry, rate))
                sources.Add(entry);

            totalSources += rate;
        }

        return totalSources;
    }

    private double GetLoadsForNode(EntityUid uid, Node node, out List<PowerMonitoringConsoleEntry> loads)
    {
        var totalLoads = 0.0d;
        loads = new List<PowerMonitoringConsoleEntry>();

        if (node.NodeGroup is not PowerNet netQ)
            return totalLoads;

        foreach (PowerConsumerComponent powerConsumer in netQ.Consumers)
        {
            var ent = powerConsumer.Owner;

            if (uid == ent)
                continue;

            if (TryMakeConsoleEntry(ent, out var entry, powerConsumer.DrawRate))
                loads.Add(entry);

            totalLoads += powerConsumer.DrawRate;
        }

        foreach (var batteryCharger in netQ.Chargers)
        {
            var ent = batteryCharger.Owner;

            if (uid == ent)
                continue;

            if (!TryComp(ent, out PowerNetworkBatteryComponent? batteryComp))
                continue;

            var rate = batteryComp.NetworkBattery.CurrentReceiving;

            if (TryMakeConsoleEntry(ent, out var entry, rate))
                loads.Add(entry);

            totalLoads += rate;
        }

        return totalLoads;
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
