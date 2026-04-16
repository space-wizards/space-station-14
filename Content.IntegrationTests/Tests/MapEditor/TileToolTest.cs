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

            // Set up tool context with active grid.
            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
                SelectedTile = new Tile(2, 0, 0), // Paint with tile type 2.
            };

            var paintTool = new PaintTool();

            // Paint at (3, 3) — should change tile from type 1 to type 2.
            paintTool.OnMouseDown(ctx, new Vector2i(3, 3));
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
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
                SelectedTile = new Tile(3, 0, 0),
            };

            var paintTool = new PaintTool();

            // Simulate a drag stroke across three tiles.
            paintTool.OnMouseDown(ctx, new Vector2i(0, 0));
            paintTool.OnMouseDrag(ctx, new Vector2i(1, 0));
            paintTool.OnMouseDrag(ctx, new Vector2i(2, 0));
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
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
            };

            var eraseTool = new EraseTool();

            eraseTool.OnMouseDown(ctx, new Vector2i(5, 5));
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

    /// <summary>
    ///     Verifies that tools use the active grid from ToolContext, not from
    ///     position-based grid detection.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task PaintTool_UsesActiveGridFromContext()
    {
        var server = Pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out mapId);

            // Create two grids.
            var grid1 = mapManager.CreateGridEntity(mapId);
            var grid1Comp = entManager.GetComponent<MapGridComponent>(grid1);
            mapSystem.SetTile(grid1, grid1Comp, new Vector2i(0, 0), new Tile(1, 0, 0));

            var grid2 = mapManager.CreateGridEntity(mapId);
            var grid2Comp = entManager.GetComponent<MapGridComponent>(grid2);
            mapSystem.SetTile(grid2, grid2Comp, new Vector2i(0, 0), new Tile(1, 0, 0));

            var commandStack = new CommandStack();

            // Set active grid to grid2 — painting should affect grid2, not grid1.
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid2,
                SelectedTile = new Tile(5, 0, 0),
            };

            var paintTool = new PaintTool();
            paintTool.OnMouseDown(ctx, new Vector2i(0, 0));
            paintTool.OnMouseUp(ctx);

            // Grid2 should be painted.
            var tile2 = mapSystem.GetTileRef(grid2, grid2Comp, new Vector2i(0, 0));
            Assert.That(tile2.Tile.TypeId, Is.EqualTo(5),
                "PaintTool should paint on the active grid (grid2)");

            // Grid1 should be unchanged.
            var tile1 = mapSystem.GetTileRef(grid1, grid1Comp, new Vector2i(0, 0));
            Assert.That(tile1.Tile.TypeId, Is.EqualTo(1),
                "Grid1 should be unaffected when grid2 is active");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }
}
