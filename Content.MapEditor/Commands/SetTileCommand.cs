using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Commands;

/// <summary>
///     Sets a single tile on a grid. Supports undo by restoring the previous tile.
/// </summary>
public sealed class SetTileCommand : IEditorCommand
{
    private readonly SharedMapSystem _mapSystem;
    private readonly EntityUid _gridUid;
    private readonly MapGridComponent _grid;
    private readonly Vector2i _position;
    private readonly Tile _oldTile;
    private readonly Tile _newTile;

    public SetTileCommand(
        SharedMapSystem mapSystem,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i position,
        Tile oldTile,
        Tile newTile)
    {
        _mapSystem = mapSystem;
        _gridUid = gridUid;
        _grid = grid;
        _position = position;
        _oldTile = oldTile;
        _newTile = newTile;
    }

    public void Execute()
    {
        _mapSystem.SetTile(_gridUid, _grid, _position, _newTile);
    }

    public void Undo()
    {
        _mapSystem.SetTile(_gridUid, _grid, _position, _oldTile);
    }
}
