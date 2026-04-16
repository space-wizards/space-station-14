using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.MapEditor.Commands;
using Content.MapEditor.Tools;
using Content.Shared.CCVar;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.MapEditor;

[TestFixture]
public sealed class SelectToolMoveTest : GameTest
{
    /// <summary>
    ///     Verifies that when a selection is dragged, both tiles AND entities move together,
    ///     and that the operation can be fully undone.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SelectTool_MoveTilesAndEntitiesTogether()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;
        EntityUid gridUid = default;
        EntityUid spawnedEntity = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);
            gridUid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(gridUid);

            // Place a tile at (5,5) with type 1.
            mapSystem.SetTile(gridUid, gridComp, new Vector2i(5, 5), new Tile(1, 0, 0));

            // Also seed a few surrounding tiles so the grid is stable.
            for (var x = 4; x <= 8; x++)
                for (var y = 4; y <= 8; y++)
                    if (x != 5 || y != 5)
                        mapSystem.SetTile(gridUid, gridComp, new Vector2i(x, y), new Tile(1, 0, 0));

            // Spawn a null-prototype entity at the center of tile (5,5).
            var tileCoords = mapSystem.GridTileToLocal(gridUid, gridComp, new Vector2i(5, 5));
            spawnedEntity = entManager.SpawnEntity(null, tileCoords);

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                ActiveGridUid = gridUid,
            };

            var selectTool = new SelectTool();

            // --- Step 1: Draw selection from (5,5) to (7,7) ---
            // This produces Selection = Box2i(5,5,8,8), covering tiles (5,5)…(7,7).
            selectTool.OnMouseDown(ctx, new Vector2i(5, 5));
            selectTool.OnMouseDrag(ctx, new Vector2i(7, 7));
            selectTool.OnMouseUp(ctx);

            Assert.That(selectTool.Selection, Is.Not.Null, "Selection should be set after drag");
            var sel = selectTool.Selection!.Value;
            Assert.That(sel.Left, Is.EqualTo(5), "Selection Left should be 5");
            Assert.That(sel.Bottom, Is.EqualTo(5), "Selection Bottom should be 5");
            Assert.That(sel.Right, Is.EqualTo(8), "Selection Right should be 8");
            Assert.That(sel.Top, Is.EqualTo(8), "Selection Top should be 8");

            // --- Step 2: Click inside selection at (6,6) to start a move ---
            selectTool.OnMouseDown(ctx, new Vector2i(6, 6));
            Assert.That(selectTool.IsMoving, Is.True, "Tool should be in move mode after clicking inside selection");

            // --- Step 3: Drag to (8,8) — net offset is (+2,+2) ---
            selectTool.OnMouseDrag(ctx, new Vector2i(8, 8));

            // --- Step 4: Release — ApplyMove fires ---
            selectTool.OnMouseUp(ctx);
            Assert.That(selectTool.IsMoving, Is.False, "Tool should exit move mode after mouse-up");

            // --- Assert tile move ---
            // Original tile at (5,5) should now be empty.
            var gridCompAfter = entManager.GetComponent<MapGridComponent>(gridUid);
            var tileAt55 = mapSystem.GetTileRef(gridUid, gridCompAfter, new Vector2i(5, 5));
            Assert.That(tileAt55.Tile.IsEmpty, Is.True, "Tile at (5,5) should be empty after move");

            // The tile should now be at (7,7) (original position + offset (2,2)).
            var tileAt77 = mapSystem.GetTileRef(gridUid, gridCompAfter, new Vector2i(7, 7));
            Assert.That(tileAt77.Tile.TypeId, Is.EqualTo(1), "Tile at (7,7) should have the original tile type after move");

            // --- Assert entity move ---
            Assert.That(entManager.EntityExists(spawnedEntity), Is.True, "Spawned entity should still exist");
            var xform = entManager.GetComponent<TransformComponent>(spawnedEntity);
            var newTile = mapSystem.CoordinatesToTile(gridUid, gridCompAfter, xform.Coordinates);
            Assert.That(newTile.X, Is.EqualTo(7), "Entity tile X should be 7 after move");
            Assert.That(newTile.Y, Is.EqualTo(7), "Entity tile Y should be 7 after move");

            // --- Step 5: Undo ---
            Assert.That(commandStack.CanUndo, Is.True, "Command stack should have an undoable command");
            commandStack.Undo();

            // After undo, tile at (5,5) should be restored.
            var gridCompUndo = entManager.GetComponent<MapGridComponent>(gridUid);
            var tileAt55Undo = mapSystem.GetTileRef(gridUid, gridCompUndo, new Vector2i(5, 5));
            Assert.That(tileAt55Undo.Tile.TypeId, Is.EqualTo(1), "Tile at (5,5) should be restored after undo");

            // Entity should be back at tile (5,5).
            Assert.That(entManager.EntityExists(spawnedEntity), Is.True, "Spawned entity should still exist after undo");
            var xformUndo = entManager.GetComponent<TransformComponent>(spawnedEntity);
            var undoTile = mapSystem.CoordinatesToTile(gridUid, gridCompUndo, xformUndo.Coordinates);
            Assert.That(undoTile.X, Is.EqualTo(5), "Entity tile X should be 5 after undo");
            Assert.That(undoTile.Y, Is.EqualTo(5), "Entity tile Y should be 5 after undo");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Regression test: after a selection is moved and then undone, clicking the
    ///     original selection region must still enter move mode so the user can move
    ///     the tiles a second time.
    ///
    ///     Bug: <see cref="SelectTool.OnMouseDrag"/> mutated <c>Selection</c> each frame
    ///     during a move to follow the cursor, so after the move finished <c>Selection</c>
    ///     pointed at the destination.  After undo, the tiles reverted to the origin but
    ///     <c>Selection</c> stayed at the destination, so
    ///     <c>Selection.Value.Contains(originalTile)</c> returned false and
    ///     <see cref="SelectTool.OnMouseDown"/> started a new drag instead of a move.
    ///
    ///     Fix: <see cref="SelectTool.OnMouseDrag"/> no longer touches <c>Selection</c>
    ///     during move mode — it only accumulates <c>_totalMoveOffset</c> for ghost
    ///     rendering.  <c>Selection</c> stays at <c>_originalSelection</c> for the
    ///     entire move gesture, so it is still valid after an undo.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task MoveAfterUndo_SecondMoveWorks()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;
        EntityUid gridUid = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);
            gridUid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(gridUid);

            // Seed a 5×5 area so the grid is stable during entity spatial queries.
            for (var x = 0; x <= 9; x++)
                for (var y = 0; y <= 9; y++)
                    mapSystem.SetTile(gridUid, gridComp, new Vector2i(x, y), new Tile(1, 0, 0));

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                ActiveGridUid = gridUid,
            };

            var tool = new SelectTool();

            // === Step 1: Draw selection at (2,2)..(3,3) → Box2i(2,2,4,4). ===
            tool.OnMouseDown(ctx, new Vector2i(2, 2));
            tool.OnMouseDrag(ctx, new Vector2i(3, 3));
            tool.OnMouseUp(ctx);

            Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(2, 2, 4, 4)),
                "Selection should be Box2i(2,2,4,4) after initial drag");

            // === Step 2: First move — click inside, drag (+3,0), release. ===
            tool.OnMouseDown(ctx, new Vector2i(3, 3));  // inside selection
            Assert.That(tool.IsMoving, Is.True, "Should enter move mode");

            tool.OnMouseDrag(ctx, new Vector2i(6, 3));  // delta = (+3, 0)
            tool.OnMouseUp(ctx);                        // ApplyMove: source=(2,2,4,4), offset=(+3,0)
            Assert.That(tool.IsMoving, Is.False, "Should exit move mode after mouse-up");

            // Tile at source should now be empty; destination should have the tile.
            var gcAfter1 = entManager.GetComponent<MapGridComponent>(gridUid);
            Assert.That(mapSystem.GetTileRef(gridUid, gcAfter1, new Vector2i(2, 2)).Tile.IsEmpty,
                Is.True, "Source tile (2,2) should be empty after first move");
            Assert.That(mapSystem.GetTileRef(gridUid, gcAfter1, new Vector2i(5, 2)).Tile.IsEmpty,
                Is.False, "Destination tile (5,2) should be filled after first move");

            // === Step 3: Undo — tiles revert to (2,2). ===
            Assert.That(commandStack.CanUndo, Is.True);
            commandStack.Undo();

            var gcUndo = entManager.GetComponent<MapGridComponent>(gridUid);
            Assert.That(mapSystem.GetTileRef(gridUid, gcUndo, new Vector2i(2, 2)).Tile.IsEmpty,
                Is.False, "Source tile (2,2) should be restored after undo");

            // Selection is still at Box2i(2,2,4,4) — the fix ensures it was never moved.
            Assert.That(tool.Selection!.Value, Is.EqualTo(new Box2i(2, 2, 4, 4)),
                "Selection must still be at the original region after undo so the second move can start");

            // === Step 4: Second move — click the original region, drag (+1,0), release. ===
            // This is the regression scenario: before the fix, Selection would have been
            // at (5,2,7,4) (the post-move destination), so clicking (3,3) would fall
            // outside it and start a new drag instead of a move.
            tool.OnMouseDown(ctx, new Vector2i(3, 3));  // must enter move mode, not drag mode
            Assert.That(tool.IsMoving, Is.True,
                "REGRESSION: clicking original selection region after undo must enter move mode, " +
                "not start a new drag — Selection must not have drifted to the destination");

            tool.OnMouseDrag(ctx, new Vector2i(4, 3));  // delta = (+1, 0)
            tool.OnMouseUp(ctx);                        // ApplyMove: source=(2,2,4,4), offset=(+1,0)

            // Verify the second move was actually applied.
            var gcAfter2 = entManager.GetComponent<MapGridComponent>(gridUid);
            Assert.That(mapSystem.GetTileRef(gridUid, gcAfter2, new Vector2i(2, 2)).Tile.IsEmpty,
                Is.True, "Source tile (2,2) should be empty after second move");
            Assert.That(mapSystem.GetTileRef(gridUid, gcAfter2, new Vector2i(3, 2)).Tile.IsEmpty,
                Is.False, "Destination tile (3,2) should be filled after second move");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }
}
