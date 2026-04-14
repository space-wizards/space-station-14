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
public sealed class FillToolTest : GameTest
{
    /// <summary>
    ///     Verifies that the FillTool fills all connected tiles of the same TypeId,
    ///     even when those tiles have different Variant values.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task FillTool_IgnoresVariantsWhenFilling()
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

            // Place a 3x1 row of tiles with the same TypeId (1) but different variants.
            mapSystem.SetTile(grid, gridComp, new Vector2i(0, 0), new Tile(1, 0, 0));
            mapSystem.SetTile(grid, gridComp, new Vector2i(1, 0), new Tile(1, 0, 1)); // variant 1
            mapSystem.SetTile(grid, gridComp, new Vector2i(2, 0), new Tile(1, 0, 2)); // variant 2

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                ActiveGridUid = grid,
                SelectedTile = new Tile(2, 0, 0), // Fill with tile type 2.
            };

            var fillTool = new FillTool();

            // Fill starting from (0, 0) — should fill all 3 tiles since they share TypeId 1.
            fillTool.OnMouseDown(ctx, new Vector2i(0, 0));

            for (var x = 0; x < 3; x++)
            {
                var tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(x, 0));
                Assert.That(tileRef.Tile.TypeId, Is.EqualTo(2),
                    $"Tile at ({x},0) should have been filled to type 2 regardless of variant");
            }

            // Undo should restore all tiles.
            Assert.That(commandStack.CanUndo, Is.True);
            commandStack.Undo();

            // Verify original TypeId is restored (we don't check variant since fill may have changed it).
            for (var x = 0; x < 3; x++)
            {
                var tileRef = mapSystem.GetTileRef(grid, gridComp, new Vector2i(x, 0));
                Assert.That(tileRef.Tile.TypeId, Is.EqualTo(1),
                    $"Tile at ({x},0) should be reverted to type 1 after undo");
            }
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }

    /// <summary>
    ///     Verifies that the FillTool does not fill across tiles with different TypeIds.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task FillTool_StopsAtDifferentTypeId()
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

            // Place tiles: type 1 at (0,0) and (1,0), type 3 at (2,0).
            mapSystem.SetTile(grid, gridComp, new Vector2i(0, 0), new Tile(1, 0, 0));
            mapSystem.SetTile(grid, gridComp, new Vector2i(1, 0), new Tile(1, 0, 0));
            mapSystem.SetTile(grid, gridComp, new Vector2i(2, 0), new Tile(3, 0, 0));

            var commandStack = new CommandStack();
            var ctx = new ToolContext
            {
                EntityManager = entManager,
                MapSystem = mapSystem,
                CommandStack = commandStack,
                ActiveGridUid = grid,
                SelectedTile = new Tile(2, 0, 0),
            };

            var fillTool = new FillTool();
            fillTool.OnMouseDown(ctx, new Vector2i(0, 0));

            // (0,0) and (1,0) should be filled.
            Assert.That(mapSystem.GetTileRef(grid, gridComp, new Vector2i(0, 0)).Tile.TypeId, Is.EqualTo(2));
            Assert.That(mapSystem.GetTileRef(grid, gridComp, new Vector2i(1, 0)).Tile.TypeId, Is.EqualTo(2));

            // (2,0) should be unchanged (different TypeId).
            Assert.That(mapSystem.GetTileRef(grid, gridComp, new Vector2i(2, 0)).Tile.TypeId, Is.EqualTo(3));
        });

        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }
}
