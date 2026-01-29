using System.Numerics;
using Robust.Client.Placement;
using Robust.Shared.Map;

namespace Content.Client.Placement.Modes;

/// <summary>
/// Placement mode for directional signs that snaps them vertically to accommodate stacking.
/// Signs are centered horizontally on tiles and snapped to 3-pixel vertical increments.
/// </summary>
public sealed class AlignDirectionalSign : PlacementMode
{
    public AlignDirectionalSign(PlacementManager pMan) : base(pMan)
    {
    }

    /// <summary>
    /// Aligns the sign placement by centering it horizontally and snapping it vertically
    /// to the nearest 3-pixel increment to prevent sprite overlap and allow half-steps.
    /// </summary>
    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        MouseCoords = ScreenToCursorGrid(mouseScreen);
        CurrentTile = GetTileRef(MouseCoords);

        if (pManager.CurrentPermission!.IsTile)
            return;

        // Center horizontally on the tile
        var x = CurrentTile.X + 0.5f;

        // Vertical snapping logic
        // Sign height is 7px. To overlap 1px borders and allow half-steps, we move in 3px steps.
        // 3 pixels * 0.03125 (1/32m) = 0.09375m.
        const float step = 0.09375f;

        // Calculate Y relative to the center of the tile
        var tileCenterY = CurrentTile.Y + 0.5f;
        var relativeY = MouseCoords.Y - tileCenterY;

        // Snap to the nearest 6-pixel increment
        var snappedY = MathF.Round(relativeY / step) * step;

        // Clamp so signs stay within the vertical bounds of the tile
        // 0.375 is 12 pixels from center (allowing a 5-sign stack comfortably)
        snappedY = Math.Clamp(snappedY, -0.375f, 0.375f);

        var y = tileCenterY + snappedY;

        MouseCoords = new EntityCoordinates(MouseCoords.EntityId,
            new Vector2(x, y) + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y));
    }

    public override bool IsValidPosition(EntityCoordinates position)
    {
        return !pManager.CurrentPermission!.IsTile && RangeCheck(position);
    }
}
