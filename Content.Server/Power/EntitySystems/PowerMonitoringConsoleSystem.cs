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
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed partial class PowerMonitoringConsoleSystem : SharedPowerMonitoringConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    private Dictionary<EntityUid, List<(EntityUid, PowerMonitoringDeviceComponent)>> _gridDevices = new();
    private Dictionary<string, Dictionary<EntityUid, EntityCoordinates>> _groupableEntityCoords = new();
    private Dictionary<EntityUid, PowerMonitoringDeviceComponent> _masterDevices = new();
    private Dictionary<EntityUid, Dictionary<Vector2i, PowerCableChunk>> _gridPowerCableChunks = new();

    //private bool _powerNetAbnormalities = false;
    private const float RoguePowerConsumerThreshold = 100000;

    // To remove
    private bool _rebuildingFocusNetwork = false;
    private bool _focusNetworkToBeRebuilt = false;

    private float _updateTimer = 1.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<ExpandPvsEvent>(OnExpandPvsEvent);
        SubscribeLocalEvent<GameRuleStartedEvent>(OnPowerGridCheckStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnPowerGridCheckEnded);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, RequestPowerMonitoringUpdateMessage>(OnUpdateRequestReceived);

        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIClosedEvent>(OnBoundUIClosed);

        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
        SubscribeLocalEvent<CableComponent, CableAnchorStateChangedEvent>(OnCableAnchorStateChanged);
        SubscribeLocalEvent<PowerMonitoringDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchoringChanged);
        SubscribeLocalEvent<PowerMonitoringDeviceComponent, NodeGroupsRebuilt>(OnNodeGroupRebuilt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            foreach (var session in _trackedSessions)
            {
                var uis = _userInterfaceSystem.GetAllUIsForSession(session);

                if (uis == null)
                    continue;

                foreach (var ui in uis)
                {
                    if (ui.UiKey is PowerMonitoringConsoleUiKey)
                    {
                        if (!EntityManager.TryGetComponent<PowerMonitoringConsoleComponent>(ui.Owner, out var console))
                            continue;

                        var consoleXform = Transform(ui.Owner);

                        if (consoleXform?.GridUid == null)
                            continue;

                        if (!_gridDevices.TryGetValue(consoleXform.GridUid.Value, out var gridDevices))
                            continue;

                        foreach ((var ent, var device) in gridDevices)
                        {
                            // Ignore joint, non-master entities
                            if (device.IsCollectionMasterOrChild && !device.IsCollectionMaster)
                                continue;

                            _pvsOverride.AddSessionOverride(EntityManager.GetNetEntity(ent), session, false);
                        }

                        UpdateUIState(ui.Owner, console, console.Focus, console.FocusGroup, session);
                    }
                }
            }
        }
    }

    public void UpdateUIState
        (EntityUid uid,
        PowerMonitoringConsoleComponent powerMonitoringConsole,
        EntityUid? focus,
        PowerMonitoringConsoleGroup? focusGroup,
        ICommonSession session)
    {
        if (!_userInterfaceSystem.TryGetUi(uid, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = Transform(uid);

        if (consoleXform?.GridUid == null)
            return;

        var gridUid = consoleXform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        if (!_gridDevices.TryGetValue(gridUid, out var gridDevices))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        EnsureComp<NavMapComponent>(gridUid);

        // Initializing data to be send to the client
        var totalSources = 0d;
        var totalBatteryUsage = 0d;
        var totalLoads = 0d;
        var allEntries = new List<PowerMonitoringConsoleEntry>();
        var sourcesForFocus = new List<PowerMonitoringConsoleEntry>();
        var loadsForFocus = new List<PowerMonitoringConsoleEntry>();
        var flags = powerMonitoringConsole.Flags;

        // Reset RoguePowerConsumer flag
        powerMonitoringConsole.Flags &= ~PowerMonitoringFlags.RoguePowerConsumer;

        // Record the load value of all non-tracked power consumers on the same grid as the console
        var powerConsumerQuery = AllEntityQuery<PowerConsumerComponent, TransformComponent>();
        while (powerConsumerQuery.MoveNext(out var ent, out var powerConsumer, out var xform))
        {
            if (xform.Anchored == false || xform.GridUid != gridUid)
                continue;

            if (TryComp<PowerMonitoringDeviceComponent>(ent, out var device))
                continue;

            // Flag an alert if power consumption is ridiculous
            if (powerConsumer.ReceivedPower >= RoguePowerConsumerThreshold)
                powerMonitoringConsole.Flags |= PowerMonitoringFlags.RoguePowerConsumer;

            totalLoads += powerConsumer.DrawRate;
        }

        if (powerMonitoringConsole.Flags != flags)
            Dirty(uid, powerMonitoringConsole);

        // Loop over all tracked devices
        foreach ((var ent, var device) in gridDevices)
        {
            // Ignore joint, non-master entities
            if (device.IsCollectionMasterOrChild && !device.IsCollectionMaster)
                continue;

            // Ignore unachored devices or those on another grid to the console
            var xform = Transform(ent);

            if (xform.Anchored == false || xform.GridUid != gridUid)
                continue;

            // Get the device power stats
            var powerValue = GetPrimaryPowerValues(ent, device, out var powerSupplied, out var powerUsage, out var batteryUsage);

            // Update all running totals
            totalSources += powerSupplied;
            totalLoads += powerUsage;
            totalBatteryUsage += batteryUsage;

            // Continue on if the device is not in the current focus group
            if (focusGroup != null && device.Group != focusGroup)
                continue;

            // Generate a new console entry with which to populate the UI
            var entry = new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), device.Group, powerValue);
            allEntries.Add(entry);
        }

        //  Reset the UI focus if it should change
        if (powerMonitoringConsole.Focus != focus)
            ResetPowerMonitoringConsoleFocus(uid, powerMonitoringConsole);

        // Update UI focus data (if applicable)
        if (focus != null)
        {
            if (TryComp<NodeContainerComponent>(focus, out var nodeContainer) &&
                TryComp<PowerMonitoringDeviceComponent>(focus, out var device))
            {
                // Record the tracked sources powering the device
                if (nodeContainer.Nodes.TryGetValue(device.SourceNode, out var sourceNode))
                    GetSourcesForNode(focus.Value, sourceNode, out sourcesForFocus);

                // Search for the enabled load node (required for portable generators)
                var loadNodeName = device.LoadNode;

                if (device.LoadNodes != null)
                {
                    var foundNode = nodeContainer.Nodes.FirstOrNull(x => x.Value is CableDeviceNode && (x.Value as CableDeviceNode)?.Enabled == true);

                    if (foundNode != null)
                        loadNodeName = foundNode.Value.Key;
                }

                // Record the tracked loads on the device
                if (nodeContainer.Nodes.TryGetValue(loadNodeName, out var loadNode))
                    GetLoadsForNode(focus.Value, loadNode, out loadsForFocus);

                // If the UI focus changed, update the highlighted power network
                if (powerMonitoringConsole.Focus != focus || _focusNetworkToBeRebuilt)
                {
                    _rebuildingFocusNetwork = true;

                    List<Node> reachableSourceNodes = sourceNode != null ? FloodFillNode(sourceNode) : new();
                    List<Node> reachableLoadNodes = loadNode != null ? FloodFillNode(loadNode) : new();

                    var reachableNodes = reachableSourceNodes.Concat(reachableLoadNodes).Select(x => x.Owner).ToList();
                    UpdateFocusNetwork(uid, powerMonitoringConsole, gridUid, mapGrid, reachableNodes);

                    _rebuildingFocusNetwork = false;
                    _focusNetworkToBeRebuilt = false;
                }
            }
        }

        powerMonitoringConsole.Focus = focus;

        // Set the UI state
        _userInterfaceSystem.SetUiState(bui,
            new PowerMonitoringConsoleBoundInterfaceState
                (totalSources,
                totalBatteryUsage,
                totalLoads,
                allEntries.ToArray(),
                sourcesForFocus.ToArray(),
                loadsForFocus.ToArray()),
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
            // This covers most power sources
            if (TryComp<PowerSupplierComponent>(uid, out var supplier))
            {
                powerValue = supplier.CurrentSupply;
                powerSupplied += powerValue;
            }

            // Edge case: radiation collectors
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

                // Records loads attached to APCs
                if (device.Group == PowerMonitoringConsoleGroup.APC && battery.Enabled)
                {
                    powerUsage += battery.NetworkBattery.LoadingNetworkDemand;
                }
            }
        }

        // Master devices add the power values from all entities they represent (if applicable)
        if (device.IsCollectionMasterOrChild && device.IsCollectionMaster)
        {
            foreach (var child in device.ChildEntities)
            {
                if (child == uid)
                    continue;

                // Safeguard to prevent infinite loops
                if (!TryComp<PowerMonitoringDeviceComponent>(child, out var childDevice) ||
                    (childDevice.IsCollectionMaster && childDevice.ChildEntities.Contains(uid)))
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
                // Combine entities represented by an master into a single entry
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedSources.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += powerSupplier.CurrentSupply;
                    indexedSources[ent] = entry;

                    continue;
                }

                indexedSources.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), entDevice.Group, powerSupplier.CurrentSupply));
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
                // Combine entities represented by an master into a single entry
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedSources.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += entBattery.CurrentSupply;
                    indexedSources[ent] = entry;

                    continue;
                }

                indexedSources.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), entDevice.Group, entBattery.CurrentSupply));
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

        // Exit if supply / demand is negligible
        if (MathHelper.CloseTo(currentDemand, 0) || MathHelper.CloseTo(currentSupply, 0))
            return;

        // Work out how much power this device (and those it represents) is actually receiving
        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            return;

        var powerUsage = battery.CurrentReceiving;

        if (TryComp<PowerMonitoringDeviceComponent>(uid, out var device) && device.IsCollectionMaster)
        {
            foreach (var child in device.ChildEntities)
            {
                if (TryComp<PowerNetworkBatteryComponent>(child, out var childBattery))
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
                // Combine entities represented by an master into a single entry
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedLoads.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += powerConsumer.ReceivedPower;
                    indexedLoads[ent] = entry;

                    continue;
                }

                indexedLoads.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), entDevice.Group, powerConsumer.ReceivedPower));
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
                // Combine entities represented by an master into a single entry
                if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                    ent = entDevice.CollectionMaster;

                if (indexedLoads.TryGetValue(ent, out var entry))
                {
                    entry.PowerValue += battery.CurrentReceiving;
                    indexedLoads[ent] = entry;

                    continue;
                }

                indexedLoads.Add(ent, new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), entDevice.Group, battery.CurrentReceiving));
            }
        }

        loads = indexedLoads.Values.ToList();

        // Exit if demand is negligible
        if (MathHelper.CloseTo(currentDemand, 0))
            return;

        var supplying = 0f;

        // Work out how much power this device (and those it represents) is actually supplying
        if (TryComp<PowerNetworkBatteryComponent>(uid, out var entBattery))
            supplying = entBattery.CurrentSupply;

        else if (TryComp<PowerSupplierComponent>(uid, out var entSupplier))
            supplying = entSupplier.CurrentSupply;

        if (TryComp<PowerMonitoringDeviceComponent>(uid, out var device) && device.IsCollectionMaster)
        {
            foreach (var child in device.ChildEntities)
            {
                if (TryComp<PowerNetworkBatteryComponent>(child, out var childBattery))
                    supplying += childBattery.CurrentSupply;

                else if (TryComp<PowerSupplierComponent>(child, out var childSupplier))
                    supplying += childSupplier.CurrentSupply;
            }
        }

        // Update the power value for each load based on the fraction of power these entities are actually draining from this device
        var powerFraction = Math.Min(supplying / currentDemand, 1f);

        foreach (var entry in loads)
            entry.PowerValue *= powerFraction;
    }

    private void ResetPowerMonitoringConsoleFocus(EntityUid uid, PowerMonitoringConsoleComponent component)
    {
        component.Focus = null;
        component.FocusChunks.Clear();

        Dirty(uid, component);
    }
}
