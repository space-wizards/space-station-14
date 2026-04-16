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
public sealed class SelectToolTest : GameTest
{
    /// <summary>
    ///     Verifies that click-and-drag with the SelectTool creates a Selection
    ///     covering the dragged region (inclusive tiles, +1 on max).
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SelectTool_DragCreatesSelection()
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

            // Seed some tiles so the grid exists.
            for (var x = 0; x < 4; x++)
                for (var y = 0; y < 4; y++)
                    mapSystem.SetTile(grid, gridComp, new Vector2i(x, y), new Tile(1, 0, 0));

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
            };

            var selectTool = new SelectTool();

            // Simulate drag from (0,0) to (2,2).
            selectTool.OnMouseDown(ctx, new Vector2i(0, 0));

            // During drag, DragStart and DragEnd should be set.
            Assert.That(selectTool.DragStart, Is.Not.Null, "DragStart should be set during drag");
            Assert.That(selectTool.DragEnd, Is.Not.Null, "DragEnd should be set during drag");
            Assert.That(selectTool.Selection, Is.Null, "Selection should be null during drag");

            selectTool.OnMouseDrag(ctx, new Vector2i(1, 1));
            selectTool.OnMouseDrag(ctx, new Vector2i(2, 2));

            Assert.That(selectTool.DragEnd, Is.EqualTo(new Vector2i(2, 2)),
                "DragEnd should track the latest drag position");

            selectTool.OnMouseUp(ctx);

            // After mouse-up, drag properties should be cleared and Selection should be set.
            Assert.That(selectTool.DragStart, Is.Null, "DragStart should be null after mouse-up");
            Assert.That(selectTool.DragEnd, Is.Null, "DragEnd should be null after mouse-up");
            Assert.That(selectTool.Selection, Is.Not.Null, "Selection should be set after drag");

            var sel = selectTool.Selection!.Value;
            // Selection should cover tiles (0,0) to (2,2) inclusive, so Box2i is (0,0,3,3).
            Assert.That(sel.Left, Is.EqualTo(0), "Selection Left should be 0");
            Assert.That(sel.Bottom, Is.EqualTo(0), "Selection Bottom should be 0");
            Assert.That(sel.Right, Is.EqualTo(3), "Selection Right should be 3 (exclusive)");
            Assert.That(sel.Top, Is.EqualTo(3), "Selection Top should be 3 (exclusive)");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that a reverse drag (bottom-right to top-left) still creates
    ///     a correctly normalized Selection.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SelectTool_ReverseDragNormalizesSelection()
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

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
            };

            var selectTool = new SelectTool();

            // Drag from (3,3) to (1,1) — reversed direction.
            selectTool.OnMouseDown(ctx, new Vector2i(3, 3));
            selectTool.OnMouseDrag(ctx, new Vector2i(1, 1));
            selectTool.OnMouseUp(ctx);

            Assert.That(selectTool.Selection, Is.Not.Null);
            var sel = selectTool.Selection!.Value;
            Assert.That(sel.Left, Is.EqualTo(1), "Normalized Left");
            Assert.That(sel.Bottom, Is.EqualTo(1), "Normalized Bottom");
            Assert.That(sel.Right, Is.EqualTo(4), "Normalized Right");
            Assert.That(sel.Top, Is.EqualTo(4), "Normalized Top");
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that DeleteSelection clears tiles in the selected region.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SelectTool_DeleteSelectionClearsTiles()
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

            // Place a 3x3 area of tiles.
            for (var x = 0; x < 3; x++)
                for (var y = 0; y < 3; y++)
                    mapSystem.SetTile(grid, gridComp, new Vector2i(x, y), new Tile(1, 0, 0));

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
            };

            var selectTool = new SelectTool();

            // Select the 3x3 area.
            selectTool.OnMouseDown(ctx, new Vector2i(0, 0));
            selectTool.OnMouseDrag(ctx, new Vector2i(2, 2));
            selectTool.OnMouseUp(ctx);

            // Delete the selection.
            selectTool.DeleteSelection(ctx);

            // All tiles should be empty.
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(x, y));
                    Assert.That(tileRef.Tile.IsEmpty, Is.True,
                        $"Tile at ({x},{y}) should be empty after delete");
                }
            }

            // Undo should restore all tiles.
            commandStack.Undo();
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(x, 0));
                    Assert.That(tileRef.Tile.TypeId, Is.EqualTo(1),
                        $"Tile at ({x},{y}) should be restored after undo");
                }
            }
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that starting a new drag clears the previous selection.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SelectTool_NewDragClearsPreviousSelection()
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

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                TileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>(),
                ActiveGridUid = grid,
            };

            var selectTool = new SelectTool();

            // First drag.
            selectTool.OnMouseDown(ctx, new Vector2i(0, 0));
            selectTool.OnMouseDrag(ctx, new Vector2i(2, 2));
            selectTool.OnMouseUp(ctx);
            Assert.That(selectTool.Selection, Is.Not.Null);

            // Start a new drag — previous selection should be cleared.
            selectTool.OnMouseDown(ctx, new Vector2i(5, 5));
            Assert.That(selectTool.Selection, Is.Null, "Previous selection should be cleared on new drag");

            selectTool.OnMouseUp(ctx);
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }
}
