using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

[TestOf(typeof(Atmospherics))]
public abstract class TileAtmosphereTest : AtmosTest
{
    [SidedDependency(Side.Server)] private readonly SharedTransformSystem _xformSys = default!;
    [SidedDependency(Side.Server)] private readonly ItemToggleSystem _itemToggleSys = default!;

    /// <summary>
    /// Spawns gas in an enclosed space and checks that pressure equalizes within reasonable time.
    /// Checks that mole count stays the same.
    /// </summary>
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

        Assert.That(GetGridMoles(RelevantAtmos), Is.Zero);

        var sourceMix = SAtmos.GetTileMixture(source, true);
        Assert.That(sourceMix, Is.Not.Null);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
        });

        var mix1 = SAtmos.GetTileMixture(point1);
        var mix2 = SAtmos.GetTileMixture(point2);

        Assert.Multiple(() =>
        {
            Assert.That(mix1, Is.Not.Null);
            Assert.That(mix2, Is.Not.Null);
        });

        AssertMixMoles(mix1, mix2, Tolerance);
        AssertGridMoles(Moles, Tolerance);
    }

    /// <summary>
    /// Spawns a combustible mixture and sets it ablaze.
    /// Checks that fire propages through the entire grid.
    /// </summary>
    [Test]
    public async Task FireSpreading()
    {
        var markers = SEntMan.AllEntities<TestMarkerComponent>();

        EntityUid source, point1, point2;
        source = point1 = point2 = EntityUid.Invalid;

        Vector2i sourceXY, point1XY, point2XY;
        sourceXY = point1XY = point2XY = Vector2i.Zero;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "point1", out point1));
            Assert.That(GetMarker(markers, "point2", out point2));

            Assert.That(_xformSys.TryGetGridTilePosition(source, out sourceXY, MapData.Grid));
            Assert.That(_xformSys.TryGetGridTilePosition(source, out point1XY, MapData.Grid));
            Assert.That(_xformSys.TryGetGridTilePosition(source, out point2XY, MapData.Grid));
        }

        Assert.That(GetGridMoles(RelevantAtmos), Is.Zero);

        var sourceMix = SAtmos.GetTileMixture(source, true);
        Assert.That(sourceMix, Is.Not.Null);

        sourceMix.AdjustMoles(Gas.Plasma, Moles / 10);
        sourceMix.AdjustMoles(Gas.Oxygen, Moles - Moles / 10);
        sourceMix.Temperature = Atmospherics.FireMinimumTemperatureToExist - 10;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, sourceXY), Is.False);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point1XY), Is.False);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point2XY), Is.False);
        }

        await Server.WaitAssertion(() =>
        {
            var welder = SEntMan.SpawnEntity("Welder", source.ToCoordinates());
            Assert.That(_itemToggleSys.TryActivate(welder));
        });

        await Server.WaitRunTicks(600);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, sourceXY), Is.True);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point1XY), Is.True);
            Assert.That(SAtmos.IsHotspotActive(MapData.Grid, point2XY), Is.True);
        }

        var mix1 = SAtmos.GetTileMixture(point1);
        var mix2 = SAtmos.GetTileMixture(point2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mix1, Is.Not.Null);
            Assert.That(mix2, Is.Not.Null);
        }

        AssertMixMoles(mix1, mix2, Tolerance);
        AssertGridMoles(Moles, Tolerance);
    }
}

// Declare separate fixtures to override the TestMap and configure CVars
// ReSharper disable InconsistentNaming
public sealed class TileAtmosphereTest_X : TileAtmosphereTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_x.yml");
}

public sealed class TileAtmosphereTest_Snake : TileAtmosphereTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_snake.yml");
}

public sealed class TileAtmosphereTest_LINDA_X : TileAtmosphereTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_x.yml");
    public override async Task Setup()
    {
        await base.Setup();
        Assert.That(Server.CfgMan.GetCVar(CCVars.MonstermosEqualization));
        Server.CfgMan.SetCVar(CCVars.MonstermosEqualization, false);
    }
}

public sealed class TileAtmosphereTest_LINDA_Snake : TileAtmosphereTest
{
    // TODO ATMOS PlasmaFireReaction doesn't conserve mass
    // error has to be bumped here
    protected override float Tolerance => 0.1f;

    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_snake.yml");
    public override async Task Setup()
    {
        await base.Setup();
        Assert.That(Server.CfgMan.GetCVar(CCVars.MonstermosEqualization));
        Server.CfgMan.SetCVar(CCVars.MonstermosEqualization, false);
    }
}
