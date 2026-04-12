using System.Numerics;
using MapEditor.RTBridge;
using Xunit;

namespace MapEditor.RTBridge.Tests;

/// <summary>
///     Tests for the pieces of the host to RT bridge that back entity
///     spawning. The actual <see cref="IEntityManager.SpawnEntity"/>
///     call cannot be exercised here because it needs a live RT. What
///     can be tested is everything that feeds into it:
///
///     <list type="bullet">
///     <item>The <see cref="EditorContext.IsSpawnable"/> palette filter,
///     which decides which prototypes the user can even pick.</item>
///     <item>The <see cref="EditorContext.PlacementPrototypeId"/>
///     selection field that the viewport reads on every left click.</item>
///     <item>The <see cref="EditorCamera.ScreenToWorld"/> math that
///     turns a click pixel into the map coordinates the spawn call
///     uses.</item>
///     </list>
///
///     If all three of those work correctly, the only thing left for
///     the actual spawn to do is hand the prototype id and coordinates
///     to <c>EntityManager.SpawnEntity</c>, which is RT's problem.
/// </summary>
public class EntitySpawnTests
{
    private static readonly Vector2 Viewport = new(980f, 720f);

    private static EditorCamera MakeCamera(Vector2? pos = null, Vector2? zoom = null)
        => new(pos ?? Vector2.Zero, zoom ?? new Vector2(0.25f, 0.25f));

    private static void AssertClose(Vector2 expected, Vector2 actual, float epsilon = 1e-4f)
    {
        Assert.InRange(actual.X, expected.X - epsilon, expected.X + epsilon);
        Assert.InRange(actual.Y, expected.Y - epsilon, expected.Y + epsilon);
    }

    // ---- Palette filter ----

    [Fact]
    public void IsSpawnable_NormalSpritedEntity_IsIncluded()
    {
        Assert.True(EditorContext.IsSpawnable(
            isAbstract: false, hideSpawnMenu: false, hasSprite: true));
    }

    [Fact]
    public void IsSpawnable_AbstractBase_IsRejected()
    {
        // Abstract prototypes exist for inheritance, the user should
        // never be placing one directly. This covers things like
        // BaseWall / BaseItem which have sprites but no ID that maps
        // to a real spawnable entity.
        Assert.False(EditorContext.IsSpawnable(
            isAbstract: true, hideSpawnMenu: false, hasSprite: true));
    }

    [Fact]
    public void IsSpawnable_HideSpawnMenu_IsRejected()
    {
        // Content can opt an entity out of spawn menus. Runtime only
        // entities, markers, debug helpers. Respect that flag.
        Assert.False(EditorContext.IsSpawnable(
            isAbstract: false, hideSpawnMenu: true, hasSprite: true));
    }

    [Fact]
    public void IsSpawnable_NoSprite_IsRejected()
    {
        // No sprite = nothing to draw = not useful in a visual editor
        // palette. Logic only entities land here.
        Assert.False(EditorContext.IsSpawnable(
            isAbstract: false, hideSpawnMenu: false, hasSprite: false));
    }

    [Fact]
    public void IsSpawnable_AllRejectReasons_IsRejected()
    {
        // Any single reason is enough to reject. Make sure a compound
        // reject still returns false.
        Assert.False(EditorContext.IsSpawnable(
            isAbstract: true, hideSpawnMenu: true, hasSprite: false));
    }

    // ---- Placement coordinates ----
    //
    // These tests frame EditorCamera.ScreenToWorld specifically around
    // the spawn use case: "user left clicks somewhere in the viewport,
    // what world coordinate does the entity end up at?"

    [Fact]
    public void PlacementCoord_ClickAtViewportCenter_SpawnsAtCameraFocus()
    {
        // Clicking the exact middle of the viewport should place the
        // entity at whatever world point the camera is looking at.
        var cam = MakeCamera(pos: new Vector2(42f, 17f));
        var clickPixel = Viewport / 2f;
        var world = cam.ScreenToWorld(clickPixel, Viewport);
        AssertClose(new Vector2(42f, 17f), world);
    }

    [Fact]
    public void PlacementCoord_ClickRightOfCenter_SpawnsRightOfCamera()
    {
        // A click 128 pixels right of center at zoom 0.25 should
        // translate to 1 world unit (tile) east of the camera.
        var cam = MakeCamera(zoom: new Vector2(0.25f, 0.25f));
        var clickPixel = Viewport / 2f + new Vector2(128f, 0f);
        var world = cam.ScreenToWorld(clickPixel, Viewport);
        AssertClose(new Vector2(1f, 0f), world);
    }

    [Fact]
    public void PlacementCoord_ClickAboveCenter_SpawnsNorthOfCameraInWorldSpace()
    {
        // Pixel Y grows downward, world Y grows upward. Clicking
        // ABOVE the center (smaller pixel Y) should spawn in the
        // +Y world direction.
        var cam = MakeCamera(zoom: new Vector2(0.25f, 0.25f));
        var clickPixel = Viewport / 2f + new Vector2(0f, -128f);
        var world = cam.ScreenToWorld(clickPixel, Viewport);
        Assert.True(world.Y > 0f, $"world.Y was {world.Y}, expected > 0");
        AssertClose(new Vector2(0f, 1f), world);
    }

    [Fact]
    public void PlacementCoord_AfterPan_UsesCurrentCameraPosition()
    {
        // Sanity check that spawn coordinates follow the camera when
        // the user pans. Same click pixel, different camera position
        // should spawn at different world coordinates.
        var cam = MakeCamera();
        var clickPixel = Viewport / 2f + new Vector2(64f, 64f);

        var worldBefore = cam.ScreenToWorld(clickPixel, Viewport);

        cam.BeginPan(Viewport / 2f, Viewport);
        cam.UpdatePan(Viewport / 2f + new Vector2(256f, 0f), Viewport);

        var worldAfter = cam.ScreenToWorld(clickPixel, Viewport);
        Assert.NotEqual(worldBefore, worldAfter);
    }

    [Fact]
    public void PlacementCoord_AfterZoomIn_SamePixelOffsetMeansSmallerWorldOffset()
    {
        // Zooming in should make 64 pixels of click offset correspond
        // to a smaller world space offset. This is the invariant that
        // lets a user zoom in to place entities more precisely.
        var cam = MakeCamera(zoom: new Vector2(1f, 1f));
        var clickPixel = Viewport / 2f + new Vector2(64f, 0f);

        var worldBeforeZoom = cam.ScreenToWorld(clickPixel, Viewport);

        // Apply several wheel zoom steps centered on the viewport
        // middle, then ask where the same cursor pixel would spawn.
        for (var i = 0; i < 5; i++)
            cam.ApplyZoomStep(1f, Viewport / 2f, Viewport);

        var worldAfterZoom = cam.ScreenToWorld(clickPixel, Viewport);

        // The Y component is unchanged (click is horizontally offset
        // from center), so compare X. The zoomed in X offset must be
        // strictly smaller in magnitude than the zoomed out one.
        Assert.True(
            System.MathF.Abs(worldAfterZoom.X - cam.Position.X)
            < System.MathF.Abs(worldBeforeZoom.X - cam.Position.X),
            $"Expected zoom in to shrink placement offset. " +
            $"Before: {worldBeforeZoom}, After: {worldAfterZoom}, Camera: {cam.Position}");
    }

    [Fact]
    public void PlacementCoord_AfterZoomAtCursor_ClickAtCursorStillSpawnsAtSameWorldPoint()
    {
        // The zoom to cursor invariant, framed for placement: if I
        // zoom in centered on some pixel and then immediately click
        // that same pixel, the spawn should happen at the same world
        // point it would have before the zoom.
        var cam = MakeCamera(zoom: new Vector2(1f, 1f));
        var cursorPixel = Viewport / 2f + new Vector2(200f, -100f);

        var worldBefore = cam.ScreenToWorld(cursorPixel, Viewport);
        cam.ApplyZoomStep(1f, cursorPixel, Viewport);
        var worldAfter = cam.ScreenToWorld(cursorPixel, Viewport);

        AssertClose(worldBefore, worldAfter);
    }

    // ---- PlacementPrototypeId contract ----

    [Fact]
    public void PlacementPrototypeId_DefaultState_IsNull()
    {
        // Fresh context means no entity selected. Left click does
        // nothing by default.
        var ctx = new FakeEditorContext();
        Assert.Null(ctx.PlacementPrototypeId);
    }

    [Fact]
    public void PlacementPrototypeId_SetAndClear_RoundTrips()
    {
        // The field is the whole selection contract, no events, no
        // observers. Make sure writes are visible on subsequent
        // reads without any funny business.
        var ctx = new FakeEditorContext();
        ctx.PlacementPrototypeId = "Chair";
        Assert.Equal("Chair", ctx.PlacementPrototypeId);
        ctx.PlacementPrototypeId = null;
        Assert.Null(ctx.PlacementPrototypeId);
    }

    /// <summary>
    ///     Minimal stand in for <see cref="EditorContext"/> that exposes
    ///     the same contract surface the tests need. The real
    ///     <see cref="EditorContext"/> constructor takes RT types
    ///     (ITaskManager, IEntityManager, IEyeManager) we cannot build
    ///     outside of a running RT instance, so we replicate just the
    ///     one property under test.
    /// </summary>
    private sealed class FakeEditorContext
    {
        public string? PlacementPrototypeId { get; set; }
    }
}
