using System.Collections.Generic;
using System.Numerics;
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
public sealed class TileToolTest : GameTest
{
    /// <summary>
    ///     Verifies that FindGridsIntersecting can locate a grid when queried
    ///     with a world position inside that grid's tile bounds.
    ///     This is the core lookup that TryResolveGridTile relies on.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task FindGridsIntersecting_FindsGridAtTilePosition()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            // Create a map and grid, then place a tile.
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(grid);
            mapSystem.SetTile(grid, gridComp, new Vector2i(0, 0), new Tile(1, 0, 0));

            // Query at tile center (0.5, 0.5) — should find the grid.
            var worldPos = new Vector2(0.5f, 0.5f);
            var pointBox = new Box2(worldPos, worldPos);
            var grids = new List<Entity<MapGridComponent>>();
            mapManager.FindGridsIntersecting(mapId, pointBox, ref grids);

            Assert.That(grids, Has.Count.GreaterThanOrEqualTo(1),
                "FindGridsIntersecting should find the grid at the tile's world position");
            Assert.That(grids[0].Owner, Is.EqualTo(grid.Owner),
                "The found grid should be the one we created");
        });

        // Clean up.
        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that FindGridsIntersecting returns zero results when
    ///     querying a position with no grid (e.g., far away from any tile).
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task FindGridsIntersecting_ReturnsEmpty_WhenNoGridAtPosition()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(grid);
            mapSystem.SetTile(grid, gridComp, new Vector2i(0, 0), new Tile(1, 0, 0));

            // Query far away — should find nothing.
            var farPos = new Vector2(1000.5f, 1000.5f);
            var pointBox = new Box2(farPos, farPos);
            var grids = new List<Entity<MapGridComponent>>();
            mapManager.FindGridsIntersecting(mapId, pointBox, ref grids);

            Assert.That(grids, Has.Count.EqualTo(0),
                "FindGridsIntersecting should return empty when no grid is at the position");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that the PaintTool sets a tile via OnMouseDown and that
    ///     OnMouseUp pushes a command onto the stack that can be undone.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task PaintTool_SetsTileAndSupportsUndo()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(grid);

            // Place an initial tile (type 1) so the grid has geometry.
            mapSystem.SetTile(grid, gridComp, new Vector2i(3, 3), new Tile(1, 0, 0));

            // Set up tool context.
            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                SelectedTile = new Tile(2, 0, 0), // Paint with tile type 2.
            };

            var paintTool = new PaintTool();

            // Paint at (3, 3) — should change tile from type 1 to type 2.
            paintTool.OnMouseDown(ctx, new Vector2i(3, 3), grid);
            paintTool.OnMouseUp(ctx);

            // Verify the tile was changed.
            var tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(3, 3));
            Assert.That(tileRef.Tile.TypeId, Is.EqualTo(2),
                "PaintTool should have set tile type to 2");

            // Verify undo restores the original tile.
            Assert.That(commandStack.CanUndo, Is.True, "Command stack should have an undoable command");
            commandStack.Undo();

            tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(3, 3));
            Assert.That(tileRef.Tile.TypeId, Is.EqualTo(1),
                "Undo should restore tile type to 1");

            // Verify redo re-applies the paint.
            commandStack.Redo();
            tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(3, 3));
            Assert.That(tileRef.Tile.TypeId, Is.EqualTo(2),
                "Redo should set tile type back to 2");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that dragging the PaintTool across multiple tiles paints them
    ///     all and groups them into a single undoable batch.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task PaintTool_DragPaintsMultipleTiles()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(grid);

            // Seed several tiles with type 1.
            for (var x = 0; x < 3; x++)
                mapSystem.SetTile(grid, gridComp, new Vector2i(x, 0), new Tile(1, 0, 0));

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                SelectedTile = new Tile(3, 0, 0),
            };

            var paintTool = new PaintTool();

            // Simulate a drag stroke across three tiles.
            paintTool.OnMouseDown(ctx, new Vector2i(0, 0), grid);
            paintTool.OnMouseDrag(ctx, new Vector2i(1, 0), grid);
            paintTool.OnMouseDrag(ctx, new Vector2i(2, 0), grid);
            paintTool.OnMouseUp(ctx);

            // All three tiles should be type 3.
            for (var x = 0; x < 3; x++)
            {
                var tile = mapSystem.GetTileRef(grid, gridComp, new Vector2i(x, 0));
                Assert.That(tile.Tile.TypeId, Is.EqualTo(3),
                    $"Tile at ({x},0) should have been painted to type 3");
            }

            // Single undo should revert all three.
            commandStack.Undo();
            for (var x = 0; x < 3; x++)
            {
                var tile = mapSystem.GetTileRef(grid, gridComp, new Vector2i(x, 0));
                Assert.That(tile.Tile.TypeId, Is.EqualTo(1),
                    $"Tile at ({x},0) should be reverted to type 1 after undo");
            }
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that the EraseTool sets a tile to empty and supports undo.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task EraseTool_RemovesTileAndSupportsUndo()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            var gridComp = entManager.GetComponent<MapGridComponent>(grid);
            mapSystem.SetTile(grid, gridComp, new Vector2i(5, 5), new Tile(1, 0, 0));

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
            };

            var eraseTool = new EraseTool();

            eraseTool.OnMouseDown(ctx, new Vector2i(5, 5), grid);
            eraseTool.OnMouseUp(ctx);

            var tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(5, 5));
            Assert.That(tileRef.Tile.IsEmpty, Is.True,
                "EraseTool should have removed the tile");

            commandStack.Undo();
            tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(5, 5));
            Assert.That(tileRef.Tile.TypeId, Is.EqualTo(1),
                "Undo should restore the erased tile");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }
}
