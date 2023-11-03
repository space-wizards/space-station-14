using Content.Server.GameTicking.Rules.Components;
using Content.Server.Pinpointer.UI;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Components;
using Content.Server.UserInterface;
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

        RefreshGrid(uid, component, xform.GridUid.Value, Comp<MapGridComponent>(xform.GridUid.Value));
    }

    private void OnDeviceAnchoringChanged(EntityUid uid, PowerMonitoringDeviceComponent component, AnchorStateChangedEvent args)
    {
        var gridUid = Transform(uid).GridUid;

        if (gridUid == null || !EntityManager.TryGetComponent<NavMapTrackableComponent>(uid, out var navMapTrackable))
            return;

        if (!_trackedDevices.TryGetValue(gridUid.Value, out var gridData))
            _trackedDevices[gridUid.Value] = new();

        if (args.Anchored)
        {
            var data = new NavMapTrackingData(EntityManager.GetNetEntity(uid), EntityManager.GetNetCoordinates(Transform(uid).Coordinates), navMapTrackable.ProtoId);
            _trackedDevices[gridUid.Value].TryAdd(uid, data);
        }

        else
        {
            _trackedDevices[gridUid.Value].Remove(uid);
        }

        var query = AllEntityQuery<PowerMonitoringConsoleComponent, NavMapTrackingConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var console, out var navMapTracking, out var xform))
        {
            if (xform.GridUid == null || !_trackedDevices.ContainsKey(gridUid.Value))
                continue;

            if (_userInterfaceSystem.IsUiOpen(ent, PowerMonitoringConsoleUiKey.Key))
            {
                navMapTracking.TrackingData = _trackedDevices[xform.GridUid.Value].Values.ToList();
                Dirty(ent, console);
            }
        }
    }

    public void OnCableAnchorStateChanged(EntityUid uid, CableComponent component, CableAnchorStateChangedEvent args)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null ||
            !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = _sharedMapSystem.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var chunkOrigin = SharedMapSystem.GetChunkIndices(tile, SharedNavMapSystem.ChunkSize);

        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var powerMonitoringConsole, out var entXform))
        {
            if (entXform.GridUid != xform.GridUid)
                return;

            if (!powerMonitoringConsole.AllChunks.TryGetValue(chunkOrigin, out var chunk))
            {
                chunk = new PowerCableChunk(chunkOrigin);
                powerMonitoringConsole.AllChunks[chunkOrigin] = chunk;
            }

            RefreshTile(ent, powerMonitoringConsole, xform.GridUid.Value, grid, chunk, tile);
        }
    }

    private void OnGridSplit(EntityUid uid, PowerMonitoringConsoleComponent component, GridSplitEvent args)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        _trackedDevices.Clear();

        var query = AllEntityQuery<PowerMonitoringDeviceComponent, NavMapTrackableComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var device, out var navMapTrackable, out var entXform))
        {
            if (entXform.GridUid == null || !entXform.Anchored)
                continue;

            if (!_trackedDevices.ContainsKey(entXform.GridUid.Value))
                _trackedDevices[entXform.GridUid.Value] = new();

            var data = new NavMapTrackingData(EntityManager.GetNetEntity(uid), EntityManager.GetNetCoordinates(Transform(uid).Coordinates), navMapTrackable.ProtoId);
            _trackedDevices[entXform.GridUid.Value].Add(ent, data);
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
    // This expansion of PVS is needed so that the sprites for these device are available to the the player UI
    // Out-of-range devices will be automatically removed from the player PVS when the UI closes
    private void OnExpandPvsEvent(ref ExpandPvsEvent ev)
    {
        var query = AllEntityQuery<PowerMonitoringConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var _, out var xform))
        {
            if (xform.GridUid == null)
                continue;

            if (_userInterfaceSystem.SessionHasOpenUi(ent, PowerMonitoringConsoleUiKey.Key, ev.Session))
            {
                if (!_trackedDevices.TryGetValue(xform.GridUid.Value, out var gridDevices))
                    continue;

                if (ev.Entities == null)
                    ev.Entities = new List<EntityUid>();

                foreach ((var device, var _) in gridDevices)
                {
                    if (Transform(device).GridUid == xform.GridUid)
                        ev.Entities.Add(device);
                }

                break;
            }
        }
    }

    private void OnUIOpened(EntityUid uid, PowerMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        var gridUid = Transform(uid).GridUid;

        if (gridUid == null || !_trackedDevices.ContainsKey(gridUid.Value))
            return;

        if (!EntityManager.TryGetComponent<NavMapTrackingConsoleComponent>(uid, out var trackingConsole))
            return;

        // Force update of all tracked entity data on opening the UI
        trackingConsole.TrackingData = _trackedDevices[gridUid.Value].Values.ToList();
        Dirty(uid, trackingConsole);
    }

    private void OnUIClosed(EntityUid uid, PowerMonitoringConsoleComponent component, BoundUIClosedEvent args)
    {
        var gridUid = Transform(uid).GridUid;

        if (gridUid == null || !_trackedDevices.ContainsKey(gridUid.Value))
            return;

        if (!EntityManager.TryGetComponent<NavMapTrackingConsoleComponent>(uid, out var trackingConsole))
            return;

        // Force clearing of all tracked entity data on closing the UI
        trackingConsole.TrackingData.Clear();
        Dirty(uid, trackingConsole);
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
