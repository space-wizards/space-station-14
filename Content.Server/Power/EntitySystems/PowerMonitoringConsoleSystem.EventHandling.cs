using Content.Server.GameTicking.Rules.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Server.GameStates;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.EntitySystems;

internal sealed partial class PowerMonitoringConsoleSystem
{
    private void OnComponentStartup(EntityUid uid, PowerMonitoringConsoleComponent component, ComponentStartup args)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        RefreshGrid(uid, component, xform.GridUid.Value, Comp<MapGridComponent>(xform.GridUid.Value));
    }

    private void OnDeviceAnchoringChanged(EntityUid uid, PowerMonitoringDeviceComponent component, AnchorStateChangedEvent args)
    {
        var gridUid = Transform(uid).GridUid;

        if (gridUid == null)
            return;

        if (!TryGetEntProtoId(uid, out var entProtoId))
            return;

        if (!_trackedDevices.TryGetValue(gridUid.Value, out var _))
            _trackedDevices[gridUid.Value] = new();

        if (args.Anchored)
        {
            _trackedDevices[gridUid.Value].Add((uid, component));

            if (component.JoinAlikeEntities)
            {
                AssignEntityToExemplarGroup(uid);
                AssignExemplarsToEntities(entProtoId.Value);
            }
        }

        else
        {
            _trackedDevices[gridUid.Value].Remove((uid, component));

            if (component.JoinAlikeEntities)
            {
                RemoveEntityFromExemplarGroup(uid);
                AssignExemplarsToEntities(entProtoId.Value);
            }
        }
    }

    public void OnCableAnchorStateChanged(EntityUid uid, CableComponent component, CableAnchorStateChangedEvent args)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = _sharedMapSystem.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, SharedNavMapSystem.ChunkSize);

        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var console, out var entXform))
        {
            if (entXform.GridUid != xform.GridUid)
                continue;

            if (!console.AllChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                console.AllChunks[chunkOrigin] = chunk;
            }

            RefreshTile(ent, console, xform.GridUid.Value, grid, chunk, tile);
        }
    }

    public void OnNodeGroupRebuilt(EntityUid uid, PowerMonitoringDeviceComponent component, NodeGroupsRebuilt args)
    {
        if (component.JoinAlikeEntities && TryGetEntProtoId(uid, out var entProtoId))
            AssignExemplarsToEntities(entProtoId.Value);

        if (_rebuildingNetwork)
            return;

        var query = AllEntityQuery<PowerMonitoringConsoleComponent>();
        while (query.MoveNext(out var ent, out var console))
        {
            if (console.Focus == uid)
                ResetPowerMonitoringConsoleFocus(ent, console);
        }
    }

    private void OnGridSplit(EntityUid uid, PowerMonitoringConsoleComponent component, GridSplitEvent args)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        _trackedDevices.Clear();

        var query = AllEntityQuery<PowerMonitoringDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var device, out var entXform))
        {
            if (entXform.GridUid == null || !entXform.Anchored)
                continue;

            if (!_trackedDevices.ContainsKey(entXform.GridUid.Value))
                _trackedDevices[entXform.GridUid.Value] = new();

            _trackedDevices[entXform.GridUid.Value].Add((ent, device));
        }

        foreach (var grid in args.NewGrids)
        {
            if (xform.GridUid == grid)
            {
                RefreshGrid(uid, component, xform.GridUid.Value, Comp<MapGridComponent>(xform.GridUid.Value));
                break;
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
                if (!_trackedDevices.TryGetValue(xform.GridUid.Value, out var gridDevices))
                    continue;

                if (ev.Entities == null)
                    ev.Entities = new List<EntityUid>();

                foreach ((var gridEnt, var device) in gridDevices)
                {
                    // Skip entities which are represented by an exemplar
                    // This will cut down the number of entities that need to be added
                    if (device.JoinAlikeEntities && !device.IsExemplar)
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
