using System;
using System.Collections.Generic;
using System.Numerics;
using Content.MapEditor.Tools;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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

    /// <summary>
    ///     When true, the SelectTool is in an active drag. The hover highlight is suppressed
    ///     so it does not visually occlude the in-progress selection rectangle.
    /// </summary>
    public bool IsDraggingSelection { get; set; }

    /// <summary>
    ///     Ghost tile positions during a selection move. Rendered as semi-transparent blue tiles
    ///     offset by <see cref="MoveGhostOffset"/>.
    /// </summary>
    public List<Vector2i>? MoveGhostTiles { get; set; }

    /// <summary>
    ///     Current offset applied to ghost tiles during a move drag.
    /// </summary>
    public Vector2i MoveGhostOffset { get; set; }

    /// <summary>
    ///     The selected entity UID (from EntitySelectTool). When set, draws a highlight
    ///     around the entity's sprite bounds.
    /// </summary>
    public EntityUid? SelectedEntityUid { get; set; }

    /// <summary>
    ///     Ghost preview texture shown at the hovered tile for placement tools.
    /// </summary>
    public Texture? PlacementPreviewTexture { get; set; }

    /// <summary>
    ///     Rotation applied to the placement preview (for pipes).
    /// </summary>
    public Angle PlacementPreviewRotation { get; set; }

    /// <summary>
    ///     When set, the placement preview renders at this grid-local position
    ///     instead of the tile center. Used for Shift free placement.
    /// </summary>
    public Vector2? FreePreviewPosition { get; set; }

    // Cyan/blue tint for selection clearly distinct from the white hover highlight.
    private static readonly Color SelectionFillColor = new(0.2f, 0.6f, 1.0f, 0.2f);
    private static readonly Color SelectionBorderColor = new(0.3f, 0.7f, 1.0f, 0.9f);
    private static readonly Color GhostTileColor = new(0.3f, 0.5f, 1.0f, 0.4f);

    // Green highlight for selected entity.
    private static readonly Color EntityHighlightFill = new(0.2f, 1.0f, 0.3f, 0.25f);
    private static readonly Color EntityHighlightBorder = new(0.2f, 1.0f, 0.3f, 0.8f);

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.SetTransform(GridWorldMatrix);

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

        // Draw placement ghost preview (semi-transparent texture of what will be placed).
        if (HoveredTile != null && PlacementPreviewTexture != null)
        {
            // Use free position if Shift is held, otherwise snap to tile center.
            var center = FreePreviewPosition ?? new Vector2(HoveredTile.Value.X + 0.5f, HoveredTile.Value.Y + 0.5f);
            var texSize = PlacementPreviewTexture.Size / (float) EyeManager.PixelsPerMeter;
            var halfSize = texSize / 2f;

            if (PlacementPreviewRotation != Angle.Zero)
            {
                // Apply rotation around the tile center.
                var rotMatrix = Matrix3Helpers.CreateTransform(center, (float) PlacementPreviewRotation.Theta);
                handle.SetTransform(Matrix3x2.Multiply(rotMatrix, GridWorldMatrix));
                var quad = new Box2(-halfSize, halfSize);
                handle.DrawTextureRect(PlacementPreviewTexture, quad, new Color(1f, 1f, 1f, 0.5f));
                handle.SetTransform(GridWorldMatrix);
            }
            else
            {
                var quad = Box2.FromDimensions(center - halfSize, texSize);
                handle.DrawTextureRect(PlacementPreviewTexture, quad, new Color(1f, 1f, 1f, 0.5f));
            }
        }

        // Draw hover highlight (suppressed during an active SelectTool drag).
        if (HoveredTile != null && !IsDraggingSelection)
        {
            var tile = HoveredTile.Value;
            var worldBox = new Box2(tile.X, tile.Y, tile.X + 1, tile.Y + 1);
            handle.DrawRect(worldBox, HighlightColor);
            handle.DrawRect(worldBox, BorderColor, filled: false);
        }

        // Draw ghost tiles during a selection move.
        if (MoveGhostTiles != null && MoveGhostTiles.Count > 0)
        {
            var offset = MoveGhostOffset;
            foreach (var pos in MoveGhostTiles)
            {
                var gx = pos.X + offset.X;
                var gy = pos.Y + offset.Y;
                var ghostBox = new Box2(gx, gy, gx + 1, gy + 1);
                handle.DrawRect(ghostBox, GhostTileColor);
            }
        }

        // Entity selection outline is handled via PostShader (set in MapEditorState),
        // not drawn here.

        // Draw selection box on top of everything so it is always visible.
        if (SelectionBox != null)
        {
            var sel = SelectionBox.Value;
            var selBox = new Box2(sel.Left, sel.Bottom, sel.Right, sel.Top);
            handle.DrawRect(selBox, SelectionFillColor);
            handle.DrawRect(selBox, SelectionBorderColor, filled: false);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
