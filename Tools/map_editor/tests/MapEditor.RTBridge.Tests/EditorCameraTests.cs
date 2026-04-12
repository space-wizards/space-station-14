using System.Numerics;
using MapEditor.RTBridge;
using Xunit;

namespace MapEditor.RTBridge.Tests;

public class EditorCameraTests
{
    // Default fixture: 980x720 viewport (the sort of size the editor actually
    // gets when the sidebar is 300 wide on a 1280-wide window), camera at
    // origin, zoom 0.25 (one tile = 128 px).
    private static readonly Vector2 Viewport = new(980f, 720f);

    private static EditorCamera MakeCamera(Vector2? pos = null, Vector2? zoom = null)
        => new(pos ?? Vector2.Zero, zoom ?? new Vector2(0.25f, 0.25f));

    private static void AssertClose(Vector2 expected, Vector2 actual, float epsilon = 1e-4f)
    {
        Assert.InRange(actual.X, expected.X - epsilon, expected.X + epsilon);
        Assert.InRange(actual.Y, expected.Y - epsilon, expected.Y + epsilon);
    }

    // --- ScreenToWorld ---

    [Fact]
    public void ScreenToWorld_Center_ReturnsCameraPosition()
    {
        var cam = MakeCamera();
        var world = cam.ScreenToWorld(Viewport / 2f, Viewport);
        AssertClose(Vector2.Zero, world);
    }

    [Fact]
    public void ScreenToWorld_CenterOffset_AccountsForCameraPosition()
    {
        var cam = MakeCamera(pos: new Vector2(10f, 20f));
        var world = cam.ScreenToWorld(Viewport / 2f, Viewport);
        AssertClose(new Vector2(10f, 20f), world);
    }

    [Fact]
    public void ScreenToWorld_FlipsYAxis()
    {
        // Moving the cursor DOWN in pixel space should move the world
        // point in -Y direction (world Y grows upward).
        var cam = MakeCamera();
        var above = cam.ScreenToWorld(Viewport / 2f + new Vector2(0, -32f), Viewport);
        var below = cam.ScreenToWorld(Viewport / 2f + new Vector2(0, 32f), Viewport);
        Assert.True(above.Y > below.Y, $"above.Y {above.Y} should exceed below.Y {below.Y}");
    }

    [Fact]
    public void ScreenToWorld_Zoom025_OneTileIs128Pixels()
    {
        // Tile pixel size = 32 px at zoom 1.0. At zoom 0.25 the camera is
        // "zoomed in 4x", so one tile should take 32/0.25 = 128 pixels on
        // screen, meaning 128 pixels of cursor movement should equal 1
        // world unit.
        var cam = MakeCamera(zoom: new Vector2(0.25f, 0.25f));
        var a = cam.ScreenToWorld(Viewport / 2f, Viewport);
        var b = cam.ScreenToWorld(Viewport / 2f + new Vector2(128f, 0f), Viewport);
        AssertClose(new Vector2(1f, 0f), b - a);
    }

    // --- Zoom ---

    [Fact]
    public void ApplyZoomStep_PositiveDelta_ZoomsIn()
    {
        var cam = MakeCamera(zoom: new Vector2(1f, 1f));
        var changed = cam.ApplyZoomStep(1f, Viewport / 2f, Viewport);
        Assert.True(changed);
        Assert.Equal(EditorCamera.ZoomStep, cam.Zoom.X, 4);
    }

    [Fact]
    public void ApplyZoomStep_NegativeDelta_ZoomsOut()
    {
        var cam = MakeCamera(zoom: new Vector2(1f, 1f));
        var changed = cam.ApplyZoomStep(-1f, Viewport / 2f, Viewport);
        Assert.True(changed);
        Assert.Equal(1f / EditorCamera.ZoomStep, cam.Zoom.X, 4);
    }

    [Fact]
    public void ApplyZoomStep_ZeroDelta_NoOp()
    {
        var cam = MakeCamera();
        var before = cam.Zoom;
        var changed = cam.ApplyZoomStep(0f, Viewport / 2f, Viewport);
        Assert.False(changed);
        Assert.Equal(before, cam.Zoom);
    }

    [Fact]
    public void ApplyZoomStep_ZoomsTowardCursor()
    {
        // Cursor in top-right quadrant, camera at origin, zoom 1.
        var cam = MakeCamera(zoom: new Vector2(1f, 1f));
        var cursor = Viewport / 2f + new Vector2(200f, -100f);

        var worldBefore = cam.ScreenToWorld(cursor, Viewport);
        cam.ApplyZoomStep(1f, cursor, Viewport);
        var worldAfter = cam.ScreenToWorld(cursor, Viewport);

        // The world point directly under the cursor must not have moved.
        AssertClose(worldBefore, worldAfter);
    }

    [Fact]
    public void ApplyZoomStep_ClampsAtMin()
    {
        var cam = MakeCamera(zoom: new Vector2(EditorCamera.MinZoom, EditorCamera.MinZoom));
        // Zoom in 50 times should bottom out, not crash or go below min.
        for (var i = 0; i < 50; i++)
            cam.ApplyZoomStep(1f, Viewport / 2f, Viewport);
        Assert.Equal(EditorCamera.MinZoom, cam.Zoom.X, 4);
    }

    [Fact]
    public void ApplyZoomStep_ClampsAtMax()
    {
        var cam = MakeCamera(zoom: new Vector2(EditorCamera.MaxZoom, EditorCamera.MaxZoom));
        for (var i = 0; i < 50; i++)
            cam.ApplyZoomStep(-1f, Viewport / 2f, Viewport);
        Assert.Equal(EditorCamera.MaxZoom, cam.Zoom.X, 4);
    }

    // --- Pan ---

    [Fact]
    public void BeginPan_SetsIsPanningAndAnchor()
    {
        var cam = MakeCamera();
        var cursor = Viewport / 2f + new Vector2(50f, -50f);
        cam.BeginPan(cursor, Viewport);
        Assert.True(cam.IsPanning);
        AssertClose(cam.ScreenToWorld(cursor, Viewport), cam.PanAnchorWorld);
    }

    [Fact]
    public void UpdatePan_WithoutBeginPan_IsNoOp()
    {
        var cam = MakeCamera(pos: new Vector2(5f, 5f));
        cam.UpdatePan(Viewport / 2f + new Vector2(100f, 0f), Viewport);
        AssertClose(new Vector2(5f, 5f), cam.Position);
    }

    [Fact]
    public void UpdatePan_KeepsAnchorUnderCursor()
    {
        // Start a pan at the center, then drag the cursor right by 200 px.
        // The camera should move LEFT so the original world center stays
        // under the cursor.
        var cam = MakeCamera(zoom: new Vector2(0.25f, 0.25f));
        cam.BeginPan(Viewport / 2f, Viewport);
        var anchor = cam.PanAnchorWorld;

        cam.UpdatePan(Viewport / 2f + new Vector2(200f, 0f), Viewport);

        // The camera should now be such that screen-to-world of the new
        // cursor position yields the original anchor.
        var worldUnderCursor = cam.ScreenToWorld(
            Viewport / 2f + new Vector2(200f, 0f), Viewport);
        AssertClose(anchor, worldUnderCursor);
    }

    [Fact]
    public void UpdatePan_MultipleSteps_IdempotentForSameCursor()
    {
        // Calling UpdatePan twice with the same cursor position must not
        // drift (the anchor is absolute, not relative). This is the bug
        // that motivated using an anchor rather than integrating deltas.
        var cam = MakeCamera();
        cam.BeginPan(Viewport / 2f, Viewport);
        cam.UpdatePan(Viewport / 2f + new Vector2(100f, 0f), Viewport);
        var positionAfterFirstUpdate = cam.Position;
        cam.UpdatePan(Viewport / 2f + new Vector2(100f, 0f), Viewport);
        AssertClose(positionAfterFirstUpdate, cam.Position);
    }

    [Fact]
    public void EndPan_ClearsIsPanning()
    {
        var cam = MakeCamera();
        cam.BeginPan(Viewport / 2f, Viewport);
        cam.EndPan();
        Assert.False(cam.IsPanning);
    }

    [Fact]
    public void PanDragRightByOneTile_MovesCameraLeftByOneTile()
    {
        // At zoom 0.25, one tile = 128 px. Dragging 128 px right should
        // move the camera 1 world unit in -X.
        var cam = MakeCamera(zoom: new Vector2(0.25f, 0.25f));
        cam.BeginPan(Viewport / 2f, Viewport);
        cam.UpdatePan(Viewport / 2f + new Vector2(128f, 0f), Viewport);
        AssertClose(new Vector2(-1f, 0f), cam.Position);
    }

    [Fact]
    public void PanDragDownByOneTile_MovesCameraUpInWorldSpace()
    {
        // Pixel Y grows downward but world Y grows upward. Dragging 128 px
        // DOWN at zoom 0.25 should move camera +1 in world Y.
        var cam = MakeCamera(zoom: new Vector2(0.25f, 0.25f));
        cam.BeginPan(Viewport / 2f, Viewport);
        cam.UpdatePan(Viewport / 2f + new Vector2(0f, 128f), Viewport);
        AssertClose(new Vector2(0f, 1f), cam.Position);
    }
}
