using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Picks the tile type under the cursor and sets it as the selected tile.
/// </summary>
public sealed class EyedropperTool : IEditorTool
{
    public string Name => "Eyedropper";

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos, EntityUid gridUid)
    {
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var tile = ctx.MapSystem.GetTileRef(gridUid, grid, tilePos).Tile;
        ctx.SelectedTile = tile;
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos, EntityUid gridUid)
    {
        // No-op: eyedropper only picks on click.
    }

    public void OnMouseUp(ToolContext ctx)
    {
        // No-op.
    }
}
