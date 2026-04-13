using System.IO;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.MapEditor;

[TestFixture]
public sealed class MapLoadSaveTest : GameTest
{
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SaveMap_ProducesValidYaml()
    {
        var pair = Pair;
        var server = pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var resManager = server.ResolveDependency<IResourceManager>();

        var savePath = new ResPath("/map_editor_save_test.yml");

        MapId mapId = default;

        await server.WaitAssertion(() =>
        {
            // Create a map with one grid and a tile.
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, new Vector2i(0, 0), new Tile(typeId: 1, flags: 1, variant: 0));

            // Ensure the directory exists for the save path.
            var dir = savePath.Directory;
            resManager.UserData.CreateDir(dir);

            // Save the map.
            Assert.That(mapLoader.TrySaveMap(mapId, savePath), "TrySaveMap should return true");
        });

        await server.WaitIdleAsync();

        // Read the saved file and verify it contains expected YAML structure.
        string yaml;
        await using (var stream = resManager.UserData.Open(savePath, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            yaml = await reader.ReadToEndAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(yaml, Is.Not.Empty, "Saved map YAML should not be empty");
            Assert.That(yaml, Does.Contain("entities:"), "YAML should contain an entities section");
            Assert.That(yaml, Does.Contain("meta:"), "YAML should contain a meta section");
        });

        // Clean up the map.
        await server.WaitPost(() => mapSystem.DeleteMap(mapId));
    }
}
