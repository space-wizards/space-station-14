using System;
using Content.MapEditor.Tools;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Client.MapEditor;

/// <summary>
///     Tests the SelectTool's click-drag lifecycle in isolation.
///     Verifies that DragStart/DragEnd/Selection are set correctly at each stage,
///     and that the overlay rectangle can be correctly derived from the tool state.
/// </summary>
[TestFixture]
public sealed class SelectToolDragTest
{
    /// <summary>
    ///     Full drag lifecycle: mouse down → drag → mouse up.
    ///     Verifies DragStart/DragEnd during drag and Selection after release.
    /// </summary>
    [Test]
    public void FullDragLifecycle()
    {
        var tool = new SelectTool();

        // Initially: no drag, no selection.
        Assert.That(tool.DragStart, Is.Null, "DragStart should be null before any input");
        Assert.That(tool.DragEnd, Is.Null, "DragEnd should be null before any input");
        Assert.That(tool.Selection, Is.Null, "Selection should be null before any input");

        // Step 1: Mouse down at (5, 5).
        tool.OnMouseDown(null!, new Vector2i(5, 5));

        Assert.That(tool.DragStart, Is.Not.Null, "DragStart should be set after OnMouseDown");
        Assert.That(tool.DragStart!.Value, Is.EqualTo(new Vector2i(5, 5)), "DragStart should be (5,5)");
        Assert.That(tool.DragEnd, Is.Not.Null, "DragEnd should be set after OnMouseDown");
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(5, 5)), "DragEnd should be (5,5) initially");
        Assert.That(tool.Selection, Is.Null, "Selection should be null during drag");

        // Step 2: Drag to (8, 8).
        tool.OnMouseDrag(null!, new Vector2i(8, 8));

        Assert.That(tool.DragStart!.Value, Is.EqualTo(new Vector2i(5, 5)), "DragStart should remain (5,5)");
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(8, 8)), "DragEnd should update to (8,8)");
        Assert.That(tool.Selection, Is.Null, "Selection should still be null during drag");

        // Step 3: Mouse up.
        tool.OnMouseUp(null!);

        Assert.That(tool.DragStart, Is.Null, "DragStart should be null after mouse up (drag ended)");
        Assert.That(tool.DragEnd, Is.Null, "DragEnd should be null after mouse up (drag ended)");
        Assert.That(tool.Selection, Is.Not.Null, "Selection should be set after mouse up");
        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(5, 5, 9, 9)),
            "Selection should be Box2i(5, 5, 9, 9) — inclusive min, exclusive max");
    }

    /// <summary>
    ///     Dragging in reverse direction (high to low coords) should normalize correctly.
    /// </summary>
    [Test]
    public void ReverseDragNormalizesSelection()
    {
        var tool = new SelectTool();

        tool.OnMouseDown(null!, new Vector2i(8, 8));
        tool.OnMouseDrag(null!, new Vector2i(5, 5));
        tool.OnMouseUp(null!);

        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(5, 5, 9, 9)),
            "Reverse drag should normalize to same box as forward drag");
    }

    /// <summary>
    ///     Starting a new drag should clear the previous selection.
    /// </summary>
    [Test]
    public void NewDragClearsPreviousSelection()
    {
        var tool = new SelectTool();

        // First drag.
        tool.OnMouseDown(null!, new Vector2i(0, 0));
        tool.OnMouseDrag(null!, new Vector2i(3, 3));
        tool.OnMouseUp(null!);
        Assert.That(tool.Selection, Is.Not.Null, "First selection should exist");

        // Second drag.
        tool.OnMouseDown(null!, new Vector2i(10, 10));
        Assert.That(tool.Selection, Is.Null, "Previous selection should be cleared on new drag start");
        Assert.That(tool.DragStart, Is.Not.Null, "New drag should start");
        Assert.That(tool.DragStart!.Value, Is.EqualTo(new Vector2i(10, 10)));
    }

    /// <summary>
    ///     Single-tile click (mouse down + immediate mouse up, no drag).
    /// </summary>
    [Test]
    public void SingleTileClick()
    {
        var tool = new SelectTool();

        tool.OnMouseDown(null!, new Vector2i(5, 5));
        tool.OnMouseUp(null!);

        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(5, 5, 6, 6)),
            "Single-tile click should produce a 1x1 selection box");
    }

    /// <summary>
    ///     Multiple drag steps should all update DragEnd correctly.
    /// </summary>
    [Test]
    public void MultipleDragSteps()
    {
        var tool = new SelectTool();

        tool.OnMouseDown(null!, new Vector2i(5, 5));

        tool.OnMouseDrag(null!, new Vector2i(6, 5));
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(6, 5)));

        tool.OnMouseDrag(null!, new Vector2i(7, 6));
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(7, 6)));

        tool.OnMouseDrag(null!, new Vector2i(8, 8));
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(8, 8)));

        // DragStart should not change.
        Assert.That(tool.DragStart!.Value, Is.EqualTo(new Vector2i(5, 5)));

        tool.OnMouseUp(null!);
        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(5, 5, 9, 9)));
    }

    /// <summary>
    ///     Simulates the overlay rectangle computation that MapEditorState.UpdateShapePreview does.
    ///     Verifies the Box2i is correctly constructed from DragStart/DragEnd during drag.
    /// </summary>
    [Test]
    public void OverlayBoxComputationDuringDrag()
    {
        var tool = new SelectTool();

        tool.OnMouseDown(null!, new Vector2i(5, 5));
        tool.OnMouseDrag(null!, new Vector2i(8, 8));

        // Simulate what UpdateShapePreview does:
        Assert.That(tool.DragStart, Is.Not.Null, "DragStart must be non-null during drag for overlay");
        Assert.That(tool.DragEnd, Is.Not.Null, "DragEnd must be non-null during drag for overlay");

        var s = tool.DragStart!.Value;
        var e = tool.DragEnd!.Value;
        var box = new Box2i(
            Math.Min(s.X, e.X), Math.Min(s.Y, e.Y),
            Math.Max(s.X, e.X) + 1, Math.Max(s.Y, e.Y) + 1);

        Assert.That(box, Is.EqualTo(new Box2i(5, 5, 9, 9)),
            "Overlay box during drag should match expected selection");
    }

    /// <summary>
    ///     After mouse up, DragStart/DragEnd should be null but Selection should be set.
    ///     The overlay code should use the Selection branch, not the DragStart/DragEnd branch.
    /// </summary>
    [Test]
    public void OverlayTransitionOnMouseUp()
    {
        var tool = new SelectTool();

        tool.OnMouseDown(null!, new Vector2i(2, 3));
        tool.OnMouseDrag(null!, new Vector2i(5, 7));

        // During drag: DragStart/DragEnd are set.
        Assert.That(tool.DragStart, Is.Not.Null);
        Assert.That(tool.DragEnd, Is.Not.Null);
        Assert.That(tool.Selection, Is.Null);

        tool.OnMouseUp(null!);

        // After mouse up: DragStart/DragEnd are null, Selection is set.
        Assert.That(tool.DragStart, Is.Null, "DragStart should be null after mouse up");
        Assert.That(tool.DragEnd, Is.Null, "DragEnd should be null after mouse up");
        Assert.That(tool.Selection, Is.Not.Null, "Selection should be set after mouse up");
        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(2, 3, 6, 8)));
    }

    /// <summary>
    ///     Calling OnMouseUp without OnMouseDown should be a no-op.
    /// </summary>
    [Test]
    public void MouseUpWithoutDownIsNoop()
    {
        var tool = new SelectTool();

        tool.OnMouseUp(null!);

        Assert.That(tool.DragStart, Is.Null);
        Assert.That(tool.DragEnd, Is.Null);
        Assert.That(tool.Selection, Is.Null);
    }

    /// <summary>
    ///     Simulates the exact frame-by-frame flow from MapEditorState.UpdateToolInput.
    ///     This tests the _lastToolTilePos gating logic that prevents OnMouseDrag
    ///     from being called when the tile hasn't changed.
    /// </summary>
    [Test]
    public void SimulateMapEditorStateFrameFlow()
    {
        var tool = new SelectTool();
        var lastToolTilePos = new Vector2i(0, 0);
        var isToolActive = false;
        var wasLeftDown = false;

        // --- Frame 0: Mouse down at (5, 5) ---
        var leftDown = true;
        var tilePos = new Vector2i(5, 5);

        if (leftDown && !wasLeftDown)
        {
            isToolActive = true;
            lastToolTilePos = tilePos;
            tool.OnMouseDown(null!, tilePos);
        }
        wasLeftDown = leftDown;

        // Verify frame 0 state.
        Assert.That(isToolActive, Is.True);
        Assert.That(tool.DragStart!.Value, Is.EqualTo(new Vector2i(5, 5)));
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(5, 5)));
        Assert.That(tool.Selection, Is.Null);

        // --- Frame 1: Mouse held, still at (5, 5) ---
        leftDown = true;
        tilePos = new Vector2i(5, 5);

        if (leftDown && !wasLeftDown)
        {
            // Would not enter (wasLeftDown is true).
        }
        else if (leftDown && isToolActive)
        {
            if (tilePos != lastToolTilePos)
            {
                lastToolTilePos = tilePos;
                tool.OnMouseDrag(null!, tilePos);
            }
        }
        wasLeftDown = leftDown;

        // DragEnd should NOT have changed (same tile).
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(5, 5)),
            "DragEnd should not change when mouse hasn't moved to a new tile");

        // --- Frame 2: Mouse moved to (7, 7) ---
        leftDown = true;
        tilePos = new Vector2i(7, 7);

        if (leftDown && !wasLeftDown)
        {
            // Would not enter.
        }
        else if (leftDown && isToolActive)
        {
            if (tilePos != lastToolTilePos)
            {
                lastToolTilePos = tilePos;
                tool.OnMouseDrag(null!, tilePos);
            }
        }
        wasLeftDown = leftDown;

        Assert.That(tool.DragStart!.Value, Is.EqualTo(new Vector2i(5, 5)));
        Assert.That(tool.DragEnd!.Value, Is.EqualTo(new Vector2i(7, 7)),
            "DragEnd should update when mouse moves to a new tile");

        // --- Frame 3: Mouse released ---
        leftDown = false;

        if (leftDown && !wasLeftDown)
        {
            // No.
        }
        else if (leftDown && isToolActive)
        {
            // No.
        }
        else if (!leftDown && isToolActive)
        {
            isToolActive = false;
            tool.OnMouseUp(null!);
        }
        wasLeftDown = leftDown;

        Assert.That(isToolActive, Is.False);
        Assert.That(tool.DragStart, Is.Null);
        Assert.That(tool.DragEnd, Is.Null);
        Assert.That(tool.Selection, Is.Not.Null);
        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(5, 5, 8, 8)));
    }

    /// <summary>
    ///     Tests that when TryResolveGridTile fails during every drag frame,
    ///     the selection ends up as a single tile (the start tile).
    ///     This simulates a scenario where the grid resolution fails during drag.
    /// </summary>
    [Test]
    public void DragWithNoGridResolution_SingleTileSelection()
    {
        var tool = new SelectTool();
        var lastToolTilePos = new Vector2i(0, 0);
        var isToolActive = false;
        var wasLeftDown = false;

        // Frame 0: Mouse down at (5, 5) — grid resolution succeeds.
        var leftDown = true;
        isToolActive = true;
        lastToolTilePos = new Vector2i(5, 5);
        tool.OnMouseDown(null!, new Vector2i(5, 5));
        wasLeftDown = leftDown;

        // Frames 1-5: Mouse held, but TryResolveGridTile fails (returns false).
        // In this case, the drag branch's inner TryResolveGridTile check fails,
        // so OnMouseDrag is never called.
        for (var i = 0; i < 5; i++)
        {
            leftDown = true;
            // Simulate TryResolveGridTile failing — we simply skip the drag logic.
            // This matches MapEditorState behavior: no OnMouseDrag call.
            wasLeftDown = leftDown;
        }

        // Frame 6: Mouse released.
        leftDown = false;
        isToolActive = false;
        tool.OnMouseUp(null!);
        wasLeftDown = leftDown;

        // Selection should be a single tile because OnMouseDrag was never called.
        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(5, 5, 6, 6)),
            "When grid resolution fails during drag, selection should be single start tile");
    }

    /// <summary>
    ///     Verifies that negative coordinate drags work correctly.
    /// </summary>
    [Test]
    public void NegativeCoordinateDrag()
    {
        var tool = new SelectTool();

        tool.OnMouseDown(null!, new Vector2i(-3, -2));
        tool.OnMouseDrag(null!, new Vector2i(1, 2));
        tool.OnMouseUp(null!);

        Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(-3, -2, 2, 3)));
    }
}
