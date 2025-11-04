using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

[TestOf(typeof(Atmospherics))]
public abstract class TileAtmosphereTest : AtmosTest
{
    [Test]
    public async Task GasSpreading()
    {
        var markers = SEntMan.AllEntities<TestMarkerComponent>();

        EntityUid source, point1, point2;
        source = point1 = point2 = EntityUid.Invalid;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "point1", out point1));
            Assert.That(GetMarker(markers, "point2", out point2));
        });

        Assert.That(GetGridMoles(RelevantAtmos), Is.EqualTo(0.0f));

        var sourceMix = SAtmos.GetTileMixture(source, true);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        var mix1 = SAtmos.GetTileMixture(point1);
        var mix2 = SAtmos.GetTileMixture(point2);

        await Pair.Server.WaitRunTicks(300);

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(mix1.TotalMoles, mix2.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            var moles = GetGridMoles(RelevantAtmos);
            Assert.That(MathHelper.CloseToPercent(moles, Moles, Tolerance), $"moles was {moles}");
        });
    }

    [Test]
    public async Task FireSpreading()
    {
        var markers = SEntMan.AllEntities<TestMarkerComponent>();

        EntityUid source, point1, point2;
        source = point1 = point2 = EntityUid.Invalid;

        Vector2i sourceXY, point1XY, point2XY;
        sourceXY = point1XY = point2XY = Vector2i.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "point1", out point1));
            Assert.That(GetMarker(markers, "point2", out point2));

            Assert.That(Transform.TryGetGridTilePosition(source, out sourceXY, MapData.Grid));
            Assert.That(Transform.TryGetGridTilePosition(source, out point1XY, MapData.Grid));
            Assert.That(Transform.TryGetGridTilePosition(source, out point2XY, MapData.Grid));
        });

        Assert.That(GetGridMoles(RelevantAtmos), Is.EqualTo(0));

        var sourceMix = SAtmos.GetTileMixture(source, true);
        sourceMix.AdjustMoles(Gas.Plasma, Moles / 10);
        sourceMix.AdjustMoles(Gas.Oxygen, Moles - Moles / 10);
        sourceMix.Temperature = Atmospherics.FireMinimumTemperatureToExist - 10;

        var mix1 = SAtmos.GetTileMixture(point1);
        var mix2 = SAtmos.GetTileMixture(point2);

        Assert.Multiple(() =>
        {
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, sourceXY), Is.False);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point1XY), Is.False);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point2XY), Is.False);
        });

        await Pair.Server.WaitAssertion(() =>
        {
            var welder = SEntMan.SpawnEntity("Welder", source.ToCoordinates());
            Assert.That(ItemToggleSys.TryActivate(welder));
        });

        await Pair.RunTicksSync(500);

        Assert.Multiple(() =>
        {
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, sourceXY), Is.True);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point1XY), Is.True);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point2XY), Is.True);
        });

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(mix1.TotalMoles, mix2.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            Assert.That(MathHelper.CloseToPercent(GetGridMoles(RelevantAtmos), Moles, Tolerance));
        });
    }
}

public sealed class TileAtmosphereTest_X : TileAtmosphereTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_x.yml");
}

public sealed class TileAtmosphereTest_Snake : TileAtmosphereTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_snake.yml");
}
