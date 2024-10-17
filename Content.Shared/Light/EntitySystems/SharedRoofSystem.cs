using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedRoofSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public void SetRoof(Entity<MapGridComponent?, RoofComponent?> grid, Vector2i index, bool value)
    {
        if (!Resolve(grid, ref grid.Comp1, ref grid.Comp2, false))
            return;

        if (!_maps.TryGetTile(grid.Comp1, index, out var tile))
            return;

        var mask = (tile.ContentFlag & (ushort)TileFlag.Roof);
        var rooved = mask != 0x0;

        if (rooved == value)
            return;

        Tile newTile;

        if (value)
        {
            newTile = tile.WithContentFlag((ushort)( tile.ContentFlag | (ushort)TileFlag.Roof));
        }
        else
        {
            newTile = tile.WithContentFlag((ushort)(tile.ContentFlag & ~(ushort)TileFlag.Roof));
        }

        _maps.SetTile((grid.Owner, grid.Comp1), index, newTile);
    }
}
