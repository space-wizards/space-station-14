using Content.Shared.FloodFill;
using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

// Process RadiationBlocker component to create rad resistance map
// When blocker is anchored it's cached in resistance map by occupied tile
public partial class SharedRadiationSystem
{
    public Dictionary<EntityUid, Dictionary<Vector2i, TileData>> _resistancePerTile = new();

    private void InitRadBlocking()
    {
        SubscribeLocalEvent<RadiationBlockerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RadiationBlockerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RadiationBlockerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<RadiationBlockerComponent, ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
    }

    private void OnInit(EntityUid uid, RadiationBlockerComponent component, ComponentInit args)
    {
        AddTile(uid, component);
    }

    private void OnShutdown(EntityUid uid, RadiationBlockerComponent component, ComponentShutdown args)
    {
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

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        _resistancePerTile.Remove(ev.EntityUid);
    }

    private void AddTile(EntityUid uid, RadiationBlockerComponent component)
    {
        // check that last position was removed
        if (component.LastPosition != null)
        {
            RemoveTile(uid, component);
        }

        // check if entity even provide some rad protection
        if (!component.Enabled || component.RadResistance <= 0)
            return;

        // check if it's on a grid
        var trs = Transform(uid);
        if (!trs.Anchored || trs.GridUid == null || !TryComp(trs.GridUid, out IMapGridComponent? grid))
            return;

        // save resistance into rad protection grid
        var gridId = trs.GridUid.Value;
        var tilePos = grid.Grid.TileIndicesFor(trs.Coordinates);
        AddToTile(gridId, tilePos, component.RadResistance);

        // and remember it as last valid position
        component.LastPosition = (gridId, tilePos);
    }

    private void RemoveTile(EntityUid uid, RadiationBlockerComponent component)
    {
        // check if blocker was placed on grid before component was removed
        if (component.LastPosition == null)
            return;
        var (gridId, tilePos) = component.LastPosition.Value;

        // try to remove
        RemoveFromTile(gridId, tilePos, component.RadResistance);
        component.LastPosition = null;
    }

    private void AddToTile(EntityUid gridId, Vector2i tilePos, float radResistance)
    {
        // get existing rad resistance grid or create it if it doesn't exist
        if (!_resistancePerTile.ContainsKey(gridId))
        {
            _resistancePerTile.Add(gridId, new Dictionary<Vector2i, TileData>());
        }
        var grid = _resistancePerTile[gridId];

        // add to existing cell more rad resistance
        var newResistance = radResistance;
        if (grid.TryGetValue(tilePos, out var existingResistance))
        {
            newResistance += existingResistance.Tolerance[0];
        }
        grid[tilePos].Tolerance[0] = newResistance;
    }

    private void RemoveFromTile(EntityUid gridId, Vector2i tilePos, float radResistance)
    {
        // get grid
        if (!_resistancePerTile.ContainsKey(gridId))
            return;
        var grid = _resistancePerTile[gridId];

        // subtract resistance from tile
        if (!grid.TryGetValue(tilePos, out var existingResistance))
            return;
        existingResistance.Tolerance[0] -= radResistance;

        // remove tile from grid if no resistance left
        if (existingResistance.Tolerance[0] > 0)
            grid[tilePos] = existingResistance;
        else
            grid.Remove(tilePos);
    }
}
