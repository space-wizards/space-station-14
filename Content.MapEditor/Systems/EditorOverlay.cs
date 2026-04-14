using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.MapEditor.Systems;

/// <summary>
///     Draws a semi-transparent highlight rectangle on the tile under the cursor.
/// </summary>
public sealed class EditorOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    /// <summary>
    ///     The tile position to highlight. Set each frame by MapEditorState.
    /// </summary>
    public Vector2i? HoveredTile { get; set; }

    /// <summary>
    ///     The world-space transform of the active grid.
    /// </summary>
    public Matrix3x2 GridWorldMatrix { get; set; } = Matrix3x2.Identity;

    /// <summary>
    ///     Fill color of the hover highlight.
    /// </summary>
    public Color HighlightColor { get; set; } = new(0.3f, 0.6f, 1.0f, 0.3f);

    /// <summary>
    ///     Border color of the hover highlight.
    /// </summary>
    public Color BorderColor { get; set; } = new(0.3f, 0.6f, 1.0f, 0.7f);

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (HoveredTile == null)
            return;

        var handle = args.WorldHandle;
        var tile = HoveredTile.Value;

        // Tiles are 1x1 in world space. Tile at (X, Y) occupies box from (X, Y) to (X+1, Y+1).
        var worldBox = new Box2(tile.X, tile.Y, tile.X + 1, tile.Y + 1);

        handle.SetTransform(GridWorldMatrix);
        handle.DrawRect(worldBox, HighlightColor);
        handle.DrawRect(worldBox, BorderColor, filled: false);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
