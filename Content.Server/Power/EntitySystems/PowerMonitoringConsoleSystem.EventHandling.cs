using Content.Server.GameTicking.Rules.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Server.GameStates;
using Robust.Shared.Map.Components;
using System.Linq;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private void OnComponentStartup(EntityUid uid, PowerMonitoringConsoleComponent component, ComponentStartup args)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        if (!_gridPowerCableChunks.ContainsKey(xform.GridUid.Value))
            RefreshPowerCableGrid(xform.GridUid.Value, Comp<MapGridComponent>(xform.GridUid.Value));

        if (!_gridPowerCableChunks.TryGetValue(xform.GridUid.Value, out var allChunks))
            return;

        component.AllChunks = allChunks;
        Dirty(uid, component);
    }

    private void OnDeviceAnchoringChanged(EntityUid uid, PowerMonitoringDeviceComponent component, AnchorStateChangedEvent args)
    {
        var gridUid = Transform(uid).GridUid;

        if (gridUid == null)
            return;

        if (!TryGetEntProtoId(uid, out var entProtoId))
            return;

        if (!_gridDevices.TryGetValue(gridUid.Value, out var _))
            _gridDevices[gridUid.Value] = new();

        if (args.Anchored)
        {
            _gridDevices[gridUid.Value].Add((uid, component));

            if (component.JoinAlikeEntities)
            {
                AssignEntityToMasterGroup(uid);
                AssignMastersToEntities(entProtoId.Value);
            }
        }

        else
        {
            _gridDevices[gridUid.Value].Remove((uid, component));

            if (component.JoinAlikeEntities)
            {
                RemoveEntityFromMasterGroup(uid);
                AssignMastersToEntities(entProtoId.Value);
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

        if (args.Anchored)
            AddPowerCableToTile(chunk, tile, component);

        else
            RemovePowerCableFromTile(chunk, tile, component);

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
        if (component.JoinAlikeEntities && TryGetEntProtoId(uid, out var entProtoId))
            AssignMastersToEntities(entProtoId.Value);

        if (_rebuildingNetwork)
            return;

        var query = AllEntityQuery<PowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var ent, out var console))
        {
            if (console.Focus == uid)
                ResetPowerMonitoringConsoleFocus(ent, console);
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

                if (!_gridDevices.ContainsKey(entXform.GridUid.Value))
                    _gridDevices[entXform.GridUid.Value] = new();

                _gridDevices[entXform.GridUid.Value].Add((ent, entDevice));

                // Note: no need to update master-child relations
                // This is handled when/if the node network is rebuilt 
            }

            var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
            var allGrids = args.NewGrids.ToList();

            if (!allGrids.Contains(args.Grid))
                allGrids.Add(args.Grid);

            // Refresh affected power cable grids
            foreach (var grid in allGrids)
                RefreshPowerCableGrid(grid, Comp<MapGridComponent>(grid));

            // Update power monitoring consoles on the updated grid
            while (query.MoveNext(out var ent, out var console, out var entXform))
            {
                foreach (var grid in allGrids)
                {
                    if (!_gridPowerCableChunks.TryGetValue(grid, out var allChunks))
                        continue;

                    if (entXform.GridUid != grid)
                        continue;

                    console.AllChunks = allChunks;
                    Dirty(ent, console);
                }
            }
        }
    }

    // Sends the list of tracked power monitoring devices to all player sessions with one or more power monitoring consoles open
    // This expansion of PVS is needed so that the metadata and sprite data for these device are available to the the player
    // Out-of-range devices will be automatically removed from the player PVS when the UI closes
    private void OnExpandPvsEvent(ref ExpandPvsEvent ev)
    {
        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var _, out var xform))
        {
            if (xform.GridUid == null)
                continue;

            // Only players with the power monitoring console UI open will have their PVS expanded
            // Note: only one player may have a given console's UI open at a time 
            if (_userInterfaceSystem.SessionHasOpenUi(ent, PowerMonitoringConsoleUiKey.Key, ev.Session))
            {
                if (!_gridDevices.TryGetValue(xform.GridUid.Value, out var gridDevices))
                    continue;

                if (ev.Entities == null)
                    ev.Entities = new List<EntityUid>();

                foreach ((var gridEnt, var device) in gridDevices)
                {
                    // Skip entities which are represented by a master
                    // This will cut down the number of entities that need to be added
                    if (device.JoinAlikeEntities && !device.IsMaster)
                        continue;

                    ev.Entities.Add(gridEnt);
                }

                break;
            }
        }
    }

    private void OnUpdateRequestReceived(EntityUid uid, PowerMonitoringConsoleComponent component, RequestPowerMonitoringUpdateMessage args)
    {
        UpdateUIState(uid, component, GetEntity(args.FocusDevice), args.FocusGroup, args.Session);
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
}
