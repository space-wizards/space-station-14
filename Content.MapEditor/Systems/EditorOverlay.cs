using System;
using System.Collections.Generic;
using System.Numerics;
using Content.MapEditor.Tools;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.MapEditor.Systems;

/// <summary>
///     Draws a semi-transparent highlight rectangle on the tile under the cursor,
///     plus shape tool preview overlays during drag operations.
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

    /// <summary>
    ///     Preview tiles to draw during a shape tool drag. Set by MapEditorState each frame.
    /// </summary>
    public List<Vector2i>? PreviewTiles { get; set; }

    /// <summary>
    ///     Fill color for preview tiles (slightly more transparent than the hover highlight).
    /// </summary>
    public Color PreviewFillColor { get; set; } = new(0.3f, 0.6f, 1.0f, 0.2f);

    /// <summary>
    ///     Border color for preview tiles.
    /// </summary>
    public Color PreviewBorderColor { get; set; } = new(0.3f, 0.6f, 1.0f, 0.5f);

    /// <summary>
    ///     Persistent selection rectangle from the SelectTool. Rendered as a filled + bordered box.
    ///     Uses Box2i where Left/Bottom is the min corner and Right/Top is max corner (exclusive).
    /// </summary>
    public Box2i? SelectionBox { get; set; }

    private static readonly Color SelectionFillColor = new(1.0f, 1.0f, 1.0f, 0.1f);
    private static readonly Color SelectionBorderColor = new(1.0f, 1.0f, 1.0f, 0.8f);

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.SetTransform(GridWorldMatrix);

        // Draw persistent selection box (behind everything else).
        if (SelectionBox != null)
        {
            var sel = SelectionBox.Value;
            var selBox = new Box2(sel.Left, sel.Bottom, sel.Right, sel.Top);
            handle.DrawRect(selBox, SelectionFillColor);
            handle.DrawRect(selBox, SelectionBorderColor, filled: false);
        }

        // Draw shape preview tiles (behind the hover highlight).
        if (PreviewTiles != null && PreviewTiles.Count > 0)
        {
            foreach (var tile in PreviewTiles)
            {
                var box = new Box2(tile.X, tile.Y, tile.X + 1, tile.Y + 1);
                handle.DrawRect(box, PreviewFillColor);
                handle.DrawRect(box, PreviewBorderColor, filled: false);
            }
        }

        // Draw hover highlight on top.
        if (HoveredTile != null)
        {
            var tile = HoveredTile.Value;
            var worldBox = new Box2(tile.X, tile.Y, tile.X + 1, tile.Y + 1);
            handle.DrawRect(worldBox, HighlightColor);
            handle.DrawRect(worldBox, BorderColor, filled: false);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
