using Content.Server.Radiation.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Radiation.Systems;

// create and update map of radiation blockers
public partial class RadiationSystem
{
    private void InitRadBlocking()
    {
        SubscribeLocalEvent<RadiationBlockerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RadiationBlockerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RadiationBlockerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<RadiationBlockerComponent, ReAnchorEvent>(OnReAnchor);

        SubscribeLocalEvent<RadiationBlockerComponent, DoorStateChangedEvent>(OnDoorChanged);

        SubscribeLocalEvent<RadiationGridResistanceComponent, EntityTerminatingEvent>(OnGridRemoved);
    }

    private void OnInit(EntityUid uid, RadiationBlockerComponent component, ComponentInit args)
    {
        if (!component.Enabled)
            return;
        AddTile(uid, component);
    }

    private void OnShutdown(EntityUid uid, RadiationBlockerComponent component, ComponentShutdown args)
    {
        if (component.Enabled)
            return;
        RemoveTile(uid, component);
    }

    private void OnAnchorChanged(EntityUid uid, RadiationBlockerComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            AddTile(uid, component);
        }
        else
        {
            RemoveTile(uid, component);
        }
    }

    private void OnReAnchor(EntityUid uid, RadiationBlockerComponent component, ref ReAnchorEvent args)
    {
        // probably grid was split
        // we need to remove entity from old resistance map
        RemoveTile(uid, component);
        // and move it to the new one
        AddTile(uid, component);
    }

    private void OnDoorChanged(EntityUid uid, RadiationBlockerComponent component, DoorStateChangedEvent args)
    {
        switch (args.State)
        {
            case DoorState.Open:
                SetEnabled(uid, false, component);
                break;
            case DoorState.Closed:
                SetEnabled(uid, true, component);
                break;
        }
    }

    private void OnGridRemoved(EntityUid uid, RadiationGridResistanceComponent component, ref EntityTerminatingEvent args)
    {
        // grid is about to be removed - lets delete grid component first
        // this should save a bit performance when blockers will be deleted
        RemComp(uid, component);
    }

    public void SetEnabled(EntityUid uid, bool isEnabled, RadiationBlockerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (isEnabled == component.Enabled)
            return;
        component.Enabled = isEnabled;

        if (!component.Enabled)
            RemoveTile(uid, component);
        else
            AddTile(uid, component);
    }

    private void AddTile(EntityUid uid, RadiationBlockerComponent component)
    {
        // check that last position was removed
        if (component.CurrentPosition != null)
        {
            RemoveTile(uid, component);
        }

        // check if entity even provide some rad protection
        if (!component.Enabled || component.RadResistance <= 0)
            return;

        // check if it's on a grid
        var trs = Transform(uid);
        if (!trs.Anchored || !TryComp(trs.GridUid, out MapGridComponent? grid))
            return;

        // save resistance into rad protection grid
        var gridId = trs.GridUid.Value;
        var tilePos = _maps.TileIndicesFor((trs.GridUid.Value, grid), trs.Coordinates);
        AddToTile(gridId, tilePos, component.RadResistance);

        // and remember it as last valid position
        component.CurrentPosition = (gridId, tilePos);
    }

    private void RemoveTile(EntityUid uid, RadiationBlockerComponent component)
    {
        // check if blocker was placed on grid before component was removed
        if (component.CurrentPosition == null)
            return;
        var (gridId, tilePos) = component.CurrentPosition.Value;

        // try to remove
        RemoveFromTile(gridId, tilePos, component.RadResistance);
        component.CurrentPosition = null;
    }

    private void AddToTile(EntityUid gridUid, Vector2i tilePos, float radResistance)
    {
        // get existing rad resistance grid or create it if it doesn't exist
        var resistance = EnsureComp<RadiationGridResistanceComponent>(gridUid);
        var grid = resistance.ResistancePerTile;

        // add to existing cell more rad resistance
        var newResistance = radResistance;
        if (grid.TryGetValue(tilePos, out var existingResistance))
        {
            newResistance += existingResistance;
        }
        grid[tilePos] = newResistance;
    }

    private void RemoveFromTile(EntityUid gridUid, Vector2i tilePos, float radResistance)
    {
        // get grid
        if (!TryComp(gridUid, out RadiationGridResistanceComponent? resistance))
            return;
        var grid = resistance.ResistancePerTile;

        // subtract resistance from tile
        if (!grid.TryGetValue(tilePos, out var existingResistance))
            return;
        existingResistance -= radResistance;

        // remove tile from grid if no resistance left
        if (existingResistance > 0)
            grid[tilePos] = existingResistance;
        else
        {
            grid.Remove(tilePos);
            if (grid.Count == 0)
                RemComp(gridUid, resistance);
        }
    }
}
