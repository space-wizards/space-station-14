using Content.Server.GameTicking.Rules.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Server.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private HashSet<ICommonSession> _trackedSessions = new();

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

    private void OnDeviceAnchoringChanged(EntityUid uid, PowerMonitoringDeviceComponent component, AnchorStateChangedEvent args)
    {
        var xfrom = Transform(uid);
        var gridUid = xfrom.GridUid;

        if (gridUid == null)
            return;

        _gridDevices.TryAdd(gridUid.Value, new());

        if (args.Anchored)
        {
            _gridDevices[gridUid.Value].Add((uid, component));

            if (component.IsCollectionMasterOrChild)
            {
                AssignEntityToMasterGroup(uid, component, xfrom.Coordinates);
                AssignMastersToEntities(component.CollectionName);
            }
        }

        else
        {
            _gridDevices[gridUid.Value].Remove((uid, component));

            if (component.IsCollectionMasterOrChild)
            {
                RemoveEntityFromMasterGroup(uid, component);
                AssignMastersToEntities(component.CollectionName);
            }
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
        while (query.MoveNext(out var ent, out var console, out var entXform))
        {
            if (entXform.GridUid != xform.GridUid)
                continue;

            console.AllChunks = allChunks;
            Dirty(ent, console);
        }
    }

    public void OnNodeGroupRebuilt(EntityUid uid, PowerMonitoringDeviceComponent component, NodeGroupsRebuilt args)
    {
        if (component.IsCollectionMasterOrChild)
            AssignMastersToEntities(component.CollectionName);

        var query = AllEntityQuery<PowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var ent, out var console))
        {
            if (console.Focus == uid)
                console.FocusChunks.Clear();
        }
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        // Reassign tracked devices sitting on the old grid to the new grids
        if (_gridDevices.TryGetValue(args.Grid, out var devicesToReassign))
        {
            _gridDevices.Remove(args.Grid);
            _gridPowerCableChunks.Remove(args.Grid);

            foreach ((var ent, var entDevice) in devicesToReassign)
            {
                var entXform = Transform(ent);

                if (entXform.GridUid == null || !entXform.Anchored)
                    continue;

                _gridDevices.TryAdd(entXform.GridUid.Value, new());
                _gridDevices[entXform.GridUid.Value].Add((ent, entDevice));

                // Note: no need to update master-child relations
                // This is handled when/if the node network is rebuilt 
            }

            var allGrids = args.NewGrids.ToList();

            if (!allGrids.Contains(args.Grid))
                allGrids.Add(args.Grid);

            // Refresh affected power cable grids
            foreach (var grid in allGrids)
                RefreshPowerCableGrid(grid, Comp<MapGridComponent>(grid));

            // Update power monitoring consoles that stand on an updated grid
            var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var console, out var entXform))
            {
                if (entXform.GridUid == null || !entXform.Anchored)
                    continue;

                if (!allGrids.Contains(entXform.GridUid.Value))
                    continue;

                if (!_gridPowerCableChunks.TryGetValue(entXform.GridUid.Value, out var allChunks))
                    continue;

                console.AllChunks = allChunks;
                Dirty(ent, console);
            }
        }
    }

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

                if (!_gridDevices.TryGetValue(xform.GridUid.Value, out var gridDevices))
                    continue;

                if (ev.Entities == null)
                    ev.Entities = new List<EntityUid>();

                foreach ((var gridEnt, var device) in gridDevices)
                {
                    // Skip entities which are represented by a collection master
                    // This will cut down the number of entities that need to be added
                    if (device.IsCollectionMasterOrChild && !device.IsCollectionMaster)
                        continue;

                    ev.Entities.Add(gridEnt);
                }
            }
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
}
