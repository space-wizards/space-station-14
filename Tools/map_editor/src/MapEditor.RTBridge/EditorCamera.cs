using System.Numerics;

namespace MapEditor.RTBridge;

/// <summary>
///     Pure logic camera controller for the editor viewport. Holds pan and
///     zoom state and exposes methods that produce new camera positions
///     given input deltas. No RT dependencies, so everything needed to
///     unit test the behavior is passed as parameters.
/// </summary>
/// <remarks>
///     Coordinate conventions match Robust Toolbox:
///     <list type="bullet">
///     <item>Pixel coordinates are control relative, origin at top left,
///     Y grows downward.</item>
///     <item>World coordinates use tile units, origin at map (0, 0),
///     Y grows <b>upward</b> (opposite of screen Y).</item>
///     <item><see cref="Zoom"/> is a scale factor, smaller = more zoomed
///     in. Default SS14 eye zoom is <c>(0.5, 0.5)</c>.</item>
///     </list>
/// </remarks>
public sealed class EditorCamera
{
    /// <summary>RT's fixed tile size in pixels (32 px = 1 world unit).</summary>
    public const float PixelsPerTile = 32f;

    /// <summary>Multiplicative factor applied per wheel notch.</summary>
    public const float ZoomStep = 0.9f;

    public const float MinZoom = 0.05f;
    public const float MaxZoom = 4f;

    /// <summary>Camera world position (the point the viewport center looks at).</summary>
    public Vector2 Position;

    /// <summary>Zoom factor, smaller values = more zoomed in.</summary>
    public Vector2 Zoom;

    /// <summary>True while a pan drag is in progress.</summary>
    public bool IsPanning { get; private set; }

    /// <summary>
    ///     World space anchor captured at <see cref="BeginPan"/>. The camera
    ///     moves so this world point stays under the cursor for the
    ///     duration of the drag.
    /// </summary>
    public Vector2 PanAnchorWorld { get; private set; }

    public EditorCamera(Vector2 position, Vector2 zoom)
    {
        Position = position;
        Zoom = zoom;
    }

    /// <summary>
    ///     Convert a control relative pixel coordinate to a world space
    ///     coordinate on the camera's map plane.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 pixel, Vector2 viewportSize)
    {
        // Center the pixel coordinate so (0,0) is the viewport middle.
        var centered = pixel - viewportSize / 2f;
        // Each pixel is Zoom / PixelsPerTile world units. Y is flipped
        // because canvas Y grows down but world Y grows up.
        return new Vector2(
            Position.X + centered.X * Zoom.X / PixelsPerTile,
            Position.Y - centered.Y * Zoom.Y / PixelsPerTile);
    }

    /// <summary>
    ///     Apply a mouse wheel zoom step centered on the given cursor
    ///     position. Keeps the world point under the cursor stationary
    ///     while the zoom factor changes (zoom to cursor behavior).
    /// </summary>
    /// <param name="wheelDeltaY">Positive = zoom in, negative = zoom out.</param>
    /// <returns>True if the zoom factor actually changed.</returns>
    public bool ApplyZoomStep(float wheelDeltaY, Vector2 cursorPixel, Vector2 viewportSize)
    {
        if (wheelDeltaY == 0f)
            return false;

        var worldBefore = ScreenToWorld(cursorPixel, viewportSize);

        var factor = wheelDeltaY > 0 ? ZoomStep : 1f / ZoomStep;
        var newZoom = Zoom * factor;
        newZoom = Vector2.Clamp(newZoom, new Vector2(MinZoom), new Vector2(MaxZoom));

        if (newZoom == Zoom)
            return false;

        Zoom = newZoom;

        // Shift the camera so worldBefore stays under the cursor.
        var worldAfter = ScreenToWorld(cursorPixel, viewportSize);
        Position -= worldAfter - worldBefore;
        return true;
    }

    /// <summary>
    ///     Begin a pan drag at the given cursor position. Captures the
    ///     world point under the cursor so <see cref="UpdatePan"/> can
    ///     keep it locked as the mouse moves.
    /// </summary>
    public void BeginPan(Vector2 cursorPixel, Vector2 viewportSize)
    {
        IsPanning = true;
        PanAnchorWorld = ScreenToWorld(cursorPixel, viewportSize);
    }

    /// <summary>
    ///     Update the camera during a pan drag. Moves the camera so the
    ///     <see cref="PanAnchorWorld"/> point is back under the cursor.
    /// </summary>
    public void UpdatePan(Vector2 cursorPixel, Vector2 viewportSize)
    {
        if (!IsPanning)
            return;

        var worldUnderCursor = ScreenToWorld(cursorPixel, viewportSize);
        Position -= worldUnderCursor - PanAnchorWorld;
    }

    public void EndPan()
    {
        IsPanning = false;
    }
}
