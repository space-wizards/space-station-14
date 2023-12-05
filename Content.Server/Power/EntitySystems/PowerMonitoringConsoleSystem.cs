using Content.Server.GameTicking.Rules.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Server.Power.NodeGroups;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
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

    // Note: this data does not need to be saved
    private HashSet<ICommonSession> _trackedSessions = new();
    private Dictionary<EntityUid, Dictionary<Vector2i, PowerCableChunk>> _gridPowerCableChunks = new();
    private float _updateTimer = 1.0f;

    private const float UpdateTime = 1.0f;
    private const float RoguePowerConsumerThreshold = 100000;

    public override void Initialize()
    {
        base.Initialize();

        // PVS events
        SubscribeLocalEvent<PowerMonitoringConsoleUserComponent, ExpandPvsEvent>(OnExpandPvsEvent);

        // Game rule events
        SubscribeLocalEvent<GameRuleStartedEvent>(OnPowerGridCheckStarted);
        SubscribeLocalEvent<GameRuleEndedEvent>(OnPowerGridCheckEnded);

        // UI events
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, PowerMonitoringConsoleMessage>(OnPowerMonitoringConsoleMessage);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, BoundUIClosedEvent>(OnBoundUIClosed);

        // Grid events
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
        SubscribeLocalEvent<CableComponent, CableAnchorStateChangedEvent>(OnCableAnchorStateChanged);
        SubscribeLocalEvent<PowerMonitoringDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchoringChanged);
        SubscribeLocalEvent<PowerMonitoringDeviceComponent, NodeGroupsRebuilt>(OnNodeGroupRebuilt);
        SubscribeLocalEvent<PowerMonitoringConsoleComponent, EntParentChangedMessage>(OnEntParentChanged);
    }

    #region EventHandling

    // Sends the list of tracked power monitoring devices to player sessions with one or more power monitoring consoles open
    // This expansion of PVS is needed so that meta and sprite data for these device are available to the the player
    // Out-of-range devices will be automatically removed from the player PVS when the UI closes
    private void OnExpandPvsEvent(EntityUid uid, PowerMonitoringConsoleUserComponent component, ref ExpandPvsEvent ev)
    {
        if (!_trackedSessions.Contains(ev.Session))
            return;

        var uis = _userInterfaceSystem.GetAllUIsForSession(ev.Session);

        if (uis == null)
            return;

        var checkedGrids = new List<EntityUid>();

        foreach (var ui in uis)
        {
            if (ui.UiKey is PowerMonitoringConsoleUiKey)
            {
                var xform = Transform(uid);

                if (xform.GridUid == null || checkedGrids.Contains(xform.GridUid.Value))
                    continue;

                checkedGrids.Add(xform.GridUid.Value);

                if (ev.Entities == null)
                    ev.Entities = new List<EntityUid>();

                var powerMonitoringDeviceQuery = AllEntityQuery<PowerMonitoringDeviceComponent, TransformComponent>();
                while (powerMonitoringDeviceQuery.MoveNext(out var ent, out var entDevice, out var entXform))
                {
                    if (entXform.GridUid != xform.GridUid)
                        continue;

                    // Skip entities which are represented by a collection master
                    // This will cut down the number of entities that need to be added
                    if (entDevice.IsCollectionMasterOrChild && !entDevice.IsCollectionMaster)
                        continue;

                    ev.Entities.Add(ent);
                }
            }
        }
    }

    private void OnPowerGridCheckStarted(ref GameRuleStartedEvent ev)
    {
        if (!TryComp<PowerGridCheckRuleComponent>(ev.RuleEntity, out var rule))
            return;

        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var console, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == rule.AffectedStation)
            {
                console.Flags |= PowerMonitoringFlags.PowerNetAbnormalities;
                Dirty(uid, console);
            }
        }
    }

    private void OnPowerGridCheckEnded(ref GameRuleEndedEvent ev)
    {
        if (!TryComp<PowerGridCheckRuleComponent>(ev.RuleEntity, out var rule))
            return;

        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var console, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == rule.AffectedStation)
            {
                console.Flags &= ~PowerMonitoringFlags.PowerNetAbnormalities;
                Dirty(uid, console);
            }
        }
    }

    private void OnPowerMonitoringConsoleMessage(EntityUid uid, PowerMonitoringConsoleComponent component, PowerMonitoringConsoleMessage args)
    {
        var focus = EntityManager.GetEntity(args.FocusDevice);
        var group = args.FocusGroup;

        // Update this if the focus device has changed
        if (component.Focus != focus)
        {
            component.Focus = focus;
            component.FocusChunks.Clear();

            if (focus == null)
                Dirty(uid, component);
        }

        // Update this if the focus group has changed
        if (component.FocusGroup != group)
        {
            component.FocusGroup = args.FocusGroup;
            Dirty(uid, component);
        }
    }

    private void OnBoundUIOpened(EntityUid uid, PowerMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        _trackedSessions.Add(args.Session);

        if (args.Session.AttachedEntity != null)
            EnsureComp<PowerMonitoringConsoleUserComponent>(args.Session.AttachedEntity.Value);
    }

    private void OnBoundUIClosed(EntityUid uid, PowerMonitoringConsoleComponent component, BoundUIClosedEvent args)
    {
        var uis = _userInterfaceSystem.GetAllUIsForSession(args.Session);

        if (uis != null)
        {
            foreach (var ui in uis)
            {
                if (ui.UiKey is PowerMonitoringConsoleUiKey)
                    return;
            }
        }

        _trackedSessions.Remove(args.Session);

        if (args.Session.AttachedEntity != null)
            EntityManager.RemoveComponent<PowerMonitoringConsoleUserComponent>(args.Session.AttachedEntity.Value);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Collect grids togethers
        var allGrids = args.NewGrids.ToList();

        if (!allGrids.Contains(args.Grid))
            allGrids.Add(args.Grid);

        // Refresh affected power cable grids
        foreach (var grid in allGrids)
            RefreshPowerCableGrid(grid, Comp<MapGridComponent>(grid));

        // Update power monitoring consoles that stand upon an updated grid
        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            if (!_gridPowerCableChunks.TryGetValue(entXform.GridUid.Value, out var allChunks))
                continue;

            entConsole.AllChunks = allChunks;
            Dirty(ent, entConsole);
        }
    }
    public void OnCableAnchorStateChanged(EntityUid uid, CableComponent component, CableAnchorStateChangedEvent args)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        if (!_gridPowerCableChunks.TryGetValue(xform.GridUid.Value, out var allChunks))
            return;

        var tile = _sharedMapSystem.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, SharedNavMapSystem.ChunkSize);

        if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
        {
            chunk = new PowerCableChunk(chunkOrigin);
            allChunks[chunkOrigin] = chunk;
        }

        var relative = SharedMapSystem.GetChunkRelative(tile, SharedNavMapSystem.ChunkSize);
        var flag = SharedNavMapSystem.GetFlag(relative);

        if (args.Anchored)
            chunk.PowerCableData[(int) component.CableType] |= flag;

        else
            chunk.PowerCableData[(int) component.CableType] &= ~flag;

        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entConsole, out var entXform))
        {
            if (entXform.GridUid != xform.GridUid)
                continue;

            entConsole.AllChunks = allChunks;
            Dirty(ent, entConsole);
        }
    }

    private void OnDeviceAnchoringChanged(EntityUid uid, PowerMonitoringDeviceComponent component, AnchorStateChangedEvent args)
    {
        var xform = Transform(uid);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return;

        if (component.IsCollectionMasterOrChild)
            AssignGridEntitiesToMaster(uid, component, null, null, xform);
    }

    public void OnNodeGroupRebuilt(EntityUid uid, PowerMonitoringDeviceComponent component, NodeGroupsRebuilt args)
    {
        if (component.IsCollectionMasterOrChild)
            AssignGridEntitiesToMaster(uid, component);

        var query = AllEntityQuery<PowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var ent, out var entConsole))
        {
            if (entConsole.Focus == uid)
                entConsole.FocusChunks.Clear();
        }
    }

    private void OnEntParentChanged(EntityUid uid, PowerMonitoringConsoleComponent component, EntParentChangedMessage args)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        // If the requested chunks are not in the dictionary, build them
        if (!_gridPowerCableChunks.TryGetValue(xform.GridUid.Value, out var allChunks))
            RefreshPowerCableGrid(xform.GridUid.Value, Comp<MapGridComponent>(xform.GridUid.Value));

        if (allChunks == null)
            return;

        component.AllChunks = allChunks;
        Dirty(uid, component);
    }

    #endregion

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

                        UpdateUIState(ui.Owner, console, session);
                    }
                }
            }
        }
    }

    public void UpdateUIState(EntityUid uid, PowerMonitoringConsoleComponent component, ICommonSession session)
    {
        if (!_userInterfaceSystem.TryGetUi(uid, PowerMonitoringConsoleUiKey.Key, out var bui))
            return;

        var consoleXform = Transform(uid);

        if (consoleXform?.GridUid == null)
            return;

        var gridUid = consoleXform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
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
        var flags = component.Flags;

        // Reset RoguePowerConsumer flag
        component.Flags &= ~PowerMonitoringFlags.RoguePowerConsumer;

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
                component.Flags |= PowerMonitoringFlags.RoguePowerConsumer;

            totalLoads += powerConsumer.DrawRate;
        }

        if (component.Flags != flags)
            Dirty(uid, component);

        // Loop over all tracked devices
        var powerMonitoringDeviceQuery = AllEntityQuery<PowerMonitoringDeviceComponent, TransformComponent>();
        while (powerMonitoringDeviceQuery.MoveNext(out var ent, out var device, out var xform))
        {
            // Ignore joint, non-master entities
            if (device.IsCollectionMasterOrChild && !device.IsCollectionMaster)
                continue;

            if (xform.Anchored == false || xform.GridUid != gridUid)
                continue;

            // Get the device power stats
            var powerValue = GetPrimaryPowerValues(ent, device, out var powerSupplied, out var powerUsage, out var batteryUsage);

            // Update all running totals
            totalSources += powerSupplied;
            totalLoads += powerUsage;
            totalBatteryUsage += batteryUsage;

            // Continue on if the device is not in the current focus group
            if (device.Group != component.FocusGroup)
                continue;

            // Generate a new console entry with which to populate the UI
            var entry = new PowerMonitoringConsoleEntry(EntityManager.GetNetEntity(ent), device.Group, powerValue);
            allEntries.Add(entry);
        }

        // Update the UI focus data (if applicable)
        if (component.Focus != null)
        {
            if (TryComp<NodeContainerComponent>(component.Focus, out var nodeContainer) &&
                TryComp<PowerMonitoringDeviceComponent>(component.Focus, out var device))
            {
                // Record the tracked sources powering the device
                if (nodeContainer.Nodes.TryGetValue(device.SourceNode, out var sourceNode))
                    GetSourcesForNode(component.Focus.Value, sourceNode, out sourcesForFocus);

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
                    GetLoadsForNode(component.Focus.Value, loadNode, out loadsForFocus);

                // If the UI focus changed, update the highlighted power network
                if (component.FocusChunks.Count == 0)
                {
                    var reachableEntities = new List<EntityUid>();

                    if (sourceNode?.NodeGroup != null)
                    {
                        foreach (var node in sourceNode.NodeGroup.Nodes)
                            reachableEntities.Add(node.Owner);
                    }

                    if (loadNode?.NodeGroup != null)
                    {
                        foreach (var node in loadNode.NodeGroup.Nodes)
                            reachableEntities.Add(node.Owner);
                    }

                    UpdateFocusNetwork(uid, component, gridUid, mapGrid, reachableEntities);
                }
            }
        }

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

        for (int i = 0; i < sources.Count; i++)
        {
            var entry = sources[i];
            sources[i] = new PowerMonitoringConsoleEntry(entry.NetEntity, entry.Group, entry.PowerValue * powerFraction);
        }
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

        for (int i = 0; i < indexedLoads.Values.Count; i++)
        {
            var entry = loads[i];
            loads[i] = new PowerMonitoringConsoleEntry(entry.NetEntity, entry.Group, entry.PowerValue * powerFraction);
        }
    }

    // Designates a supplied entity as a 'collection master'. Other entities which share this 
    // entities collection name and are attached on the same load network are assigned this entity 
    // as the master that represents them on the console UI. This way you can have one device
    // represent multiple connected devices
    private void AssignGridEntitiesToMaster
        (EntityUid uid,
        PowerMonitoringDeviceComponent? device = null,
        NodeContainerComponent? nodeContainer = null,
        NavMapTrackableComponent? trackable = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref device, ref nodeContainer, ref trackable, ref xform, false))
            return;

        // If the device is not attached to a network, exit
        if (!nodeContainer.Nodes.TryGetValue(device.LoadNode, out var loadNode) ||
            loadNode.ReachableNodes.Count == 0)
        {
            // Make a child the new master of the collection if necessary
            if (device.ChildEntities.Count > 0)
                AssignGridEntitiesToMaster(device.ChildEntities.First());

            device.CollectionMaster = uid;
            device.ChildEntities.Clear();

            trackable.ParentUid = null;
            trackable.ChildOffsets.Clear();
            Dirty(uid, trackable);

            return;
        }

        device.CollectionMaster = uid;
        device.ChildEntities.Clear();

        trackable.ParentUid = null;
        trackable.ChildOffsets.Clear();

        var query = AllEntityQuery<PowerMonitoringDeviceComponent, NodeContainerComponent, NavMapTrackableComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entDevice, out var entNodeContainer, out var entTrackable, out var entXform))
        {
            if (ent == uid)
                continue;

            if (!entNodeContainer.Nodes.TryGetValue(entDevice.LoadNode, out var entLoadNode) ||
                entLoadNode.ReachableNodes.Count == 0)
                continue;

            // Matching netIds - this device should be represented by the master
            if ((loadNode.NodeGroup as BaseNodeGroup)?.NetId == (entLoadNode.NodeGroup as BaseNodeGroup)?.NetId)
            {
                device.ChildEntities.Add(ent);
                trackable.ChildOffsets.Add(entXform.Coordinates - xform.Coordinates);

                entDevice.CollectionMaster = uid;
                entTrackable.ParentUid = uid;
                entTrackable.ChildOffsets.Clear();
                Dirty(ent, entTrackable);
            }
        }

        Dirty(uid, trackable);
    }

    private void RefreshPowerCableGrid(EntityUid gridUid, MapGridComponent grid)
    {
        // Clears all chunks for the associated grid
        var allChunks = new Dictionary<Vector2i, PowerCableChunk>();
        _gridPowerCableChunks[gridUid] = allChunks;

        // Adds all power cables to the grid
        var query = AllEntityQuery<CableComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var cable, out var entXform))
        {
            if (entXform.GridUid != gridUid)
                continue;

            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, entXform.Coordinates);
            var chunkOrigin = SharedMapSystem.GetChunkIndices(tile.GridIndices, SharedNavMapSystem.ChunkSize);

            if (!allChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                allChunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(tile.GridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);

            chunk.PowerCableData[(int) cable.CableType] |= flag;
        }
    }

    private void UpdateFocusNetwork(EntityUid uid, PowerMonitoringConsoleComponent component, EntityUid gridUid, MapGridComponent grid, List<EntityUid> nodeList)
    {
        component.FocusChunks.Clear();

        foreach (var ent in nodeList)
        {
            var xform = Transform(ent);
            var tile = _sharedMapSystem.GetTileRef(gridUid, grid, xform.Coordinates);
            var gridIndices = tile.GridIndices;
            var chunkOrigin = SharedMapSystem.GetChunkIndices(gridIndices, SharedNavMapSystem.ChunkSize);

            if (!component.FocusChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                component.FocusChunks[chunkOrigin] = chunk;
            }

            var relative = SharedMapSystem.GetChunkRelative(gridIndices, SharedNavMapSystem.ChunkSize);
            var flag = SharedNavMapSystem.GetFlag(relative);

            if (TryComp<CableComponent>(ent, out var cable))
                chunk.PowerCableData[(int) cable.CableType] |= flag;
        }

        Dirty(uid, component);
    }
}
