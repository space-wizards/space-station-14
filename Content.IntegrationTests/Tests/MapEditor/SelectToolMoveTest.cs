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
}
