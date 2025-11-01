using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tests;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.IntegrationTests.Tests.Atmos;

[TestOf(typeof(Atmospherics))]
public sealed class TileAtmosphereTest
{
    private const string TestMap1 = "Maps/Test/Atmospherics/tile_atmosphere_test_x.yml";
    private const string TestMap2 = "Maps/Test/Atmospherics/tile_atmosphere_test_snake.yml";

    private const float Moles = 1000.0f;

    // 5% is a lot, but it can get this bad ATM...
    private const float Tolerance = 0.05f;

    [Test]
    [TestCase(TestMap1)]
    [TestCase(TestMap2)]
    public async Task TileAtmosphere(string mapPath)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entityManager = server.EntMan;
        var mapLoader = entityManager.System<MapLoaderSystem>();
        var atmosSystem = entityManager.System<AtmosphereSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
        Entity<MapComponent>? mapID = default;

        // Load our test map in and assert that it exists.
        await server.WaitPost(() =>
        {
            var map = new ResPath(mapPath);
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(map, out mapID, out var gridSet, deserializationOptions),
                $"Failed to load map {map}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045
            grid = gridSet.First();
        });

        await server.WaitRunTicks(10);

        Entity<GridAtmosphereComponent> relevantAtmos = (grid, entityManager.GetComponent<GridAtmosphereComponent>(grid));

        var markers = entityManager.AllEntities<TestMarkerComponent>();

        EntityUid source, point1, point2;
        source = point1 = point2 = EntityUid.Invalid;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "point1", out point1));
            Assert.That(GetMarker(markers, "point2", out point2));
        });

        Assert.That(GetGridMoles(relevantAtmos), Is.EqualTo(0.0f));

        var sourceMix = atmosSystem.GetTileMixture(source, true);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        var mix1 = atmosSystem.GetTileMixture(point1);
        var mix2 = atmosSystem.GetTileMixture(point2);

        await server.WaitRunTicks(300);

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(mix1.TotalMoles, mix2.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            var moles = GetGridMoles(relevantAtmos);
            Assert.That(MathHelper.CloseToPercent(moles, Moles, Tolerance), $"moles was {moles}");
        });

        await server.WaitAssertion(() =>
        {
            entityManager.DeleteEntity(mapID);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    [TestCase(TestMap1)]
    [TestCase(TestMap2)]
    public async Task FireSpreading(string mapPath)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        var entityManager = server.EntMan;
        var mapLoader = entityManager.System<MapLoaderSystem>();
        var atmosSystem = entityManager.System<AtmosphereSystem>();
        var itemToggleSystem = entityManager.System<ItemToggleSystem>();
        var transformSystem = entityManager.System<SharedTransformSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
        Entity<MapComponent>? mapID = default;

        // Load our test map in and assert that it exists.
        await server.WaitPost(() =>
        {
            var map = new ResPath(mapPath);
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(map, out mapID, out var gridSet, deserializationOptions),
                $"Failed to load map {map}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045
            grid = gridSet.First();
        });

        await server.WaitRunTicks(10);

        Entity<GridAtmosphereComponent> relevantAtmos = (grid, entityManager.GetComponent<GridAtmosphereComponent>(grid));

        var markers = entityManager.AllEntities<TestMarkerComponent>();

        EntityUid source, point1, point2;
        source = point1 = point2 = EntityUid.Invalid;

        Vector2i sourceXY, point1XY, point2XY;
        sourceXY = point1XY = point2XY = Vector2i.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "point1", out point1));
            Assert.That(GetMarker(markers, "point2", out point2));

            Assert.That(transformSystem.TryGetGridTilePosition(source, out sourceXY, grid));
            Assert.That(transformSystem.TryGetGridTilePosition(source, out point1XY, grid));
            Assert.That(transformSystem.TryGetGridTilePosition(source, out point2XY, grid));
        });

        Assert.That(GetGridMoles(relevantAtmos), Is.EqualTo(0));

        var sourceMix = atmosSystem.GetTileMixture(source, true);
        sourceMix.AdjustMoles(Gas.Plasma, Moles / 10);
        sourceMix.AdjustMoles(Gas.Oxygen, Moles - Moles / 10);
        sourceMix.Temperature = Atmospherics.FireMinimumTemperatureToExist - 10;

        var mix1 = atmosSystem.GetTileMixture(point1);
        var mix2 = atmosSystem.GetTileMixture(point2);

        Assert.Multiple(() =>
        {
            Assert.That(atmosSystem.IsHotspotActive(grid, sourceXY), Is.False);
            Assert.That(atmosSystem.IsHotspotActive(grid, point1XY), Is.False);
            Assert.That(atmosSystem.IsHotspotActive(grid, point2XY), Is.False);
        });

        await server.WaitAssertion(() =>
        {
            var welder = entityManager.SpawnEntity("Welder", source.ToCoordinates());
            Assert.That(itemToggleSystem.TryActivate(welder));
        });

        await pair.RunTicksSync(500);

        Assert.Multiple(() =>
        {
            Assert.That(atmosSystem.IsHotspotActive(grid, sourceXY), Is.True);
            Assert.That(atmosSystem.IsHotspotActive(grid, point1XY), Is.True);
            Assert.That(atmosSystem.IsHotspotActive(grid, point2XY), Is.True);
        });

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(mix1.TotalMoles, mix2.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            Assert.That(MathHelper.CloseToPercent(GetGridMoles(relevantAtmos), Moles, Tolerance));
        });

        await server.WaitAssertion(() =>
        {
            entityManager.DeleteEntity(mapID);
        });

        await pair.CleanReturnAsync();
    }

    private static bool GetMarker(Entity<TestMarkerComponent>[] markers, string id, out EntityUid marker)
    {
        foreach (var ent in markers)
        {
            if (ent.Comp.Id == id)
            {
                marker = ent;
                return true;
            }
        }
        marker = EntityUid.Invalid;
        return false;
    }

    private static float GetGridMoles(Entity<GridAtmosphereComponent> grid)
    {
        var moles = 0.0f;
        foreach (var tile in grid.Comp.Tiles.Values)
        {
            moles += tile.Air?.TotalMoles ?? 0.0f;
        }

        return moles;
    }
}
