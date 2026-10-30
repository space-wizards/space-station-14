using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Maps;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

public sealed class RoomSpacingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_room.yml");

    [SidedDependency(Side.Server)] private readonly EntityLookupSystem _sLookupSystem = default!;
    [SidedDependency(Side.Server)] private readonly SharedMapSystem _sMapSystem = default!;
    [SidedDependency(Side.Server)] private readonly ITileDefinitionManager _tileDefManager = default!;

    private readonly ProtoId<ContentTileDefinition> _latticeDefinition = new("Lattice");

    /// <summary>
    /// Checks that deleting an outer wall spaces the room.
    /// </summary>
    [Test]
    public async Task DeleteWall()
    {
        var markers = SEntMan.AllEntities<TestMarkerComponent>();

        EntityUid source, floor, wallPos;
        source = floor = wallPos = EntityUid.Invalid;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "floor", out floor));
            Assert.That(GetMarker(markers, "wall", out wallPos));
        });

        var lookup = _sLookupSystem.GetEntitiesIntersecting(wallPos);
        var wall = lookup.FirstOrNull();
        Assert.That(wall, Is.Not.Null);

        Assert.That(GetGridMoles(RelevantAtmos), Is.Zero);

        var sourceMix = SAtmos.GetTileMixture(source, true);
        Assert.That(sourceMix, Is.Not.Null);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Server.WaitRunTicks(500);

        var mix1 = SAtmos.GetTileMixture(floor);
        Assert.That(mix1, Is.Not.Null);

        AssertMixMoles(sourceMix, mix1, Tolerance);
        AssertGridMoles(Moles, Tolerance);

        // Space the room
        await Server.WaitAssertion(() =>
        {
            SEntMan.DeleteEntity(wall);
        });

        await Server.WaitRunTicks(10);

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
            }
        });

        AssertMixMoles(sourceMix, mix1, Tolerance);
        AssertGridMoles(0, Tolerance);
    }

    /// <summary>
    /// Checks that exposing tile lattice spaces the room.
    /// </summary>
    [Test]
    public async Task PryLattice()
    {
        var markers = SEntMan.AllEntities<TestMarkerComponent>();

        EntityUid source, floor, wallPos;
        source = floor = wallPos = EntityUid.Invalid;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "floor", out floor));
            Assert.That(GetMarker(markers, "wall", out wallPos));
        });

        var lookup = _sLookupSystem.GetEntitiesIntersecting(wallPos);
        var wall = lookup.FirstOrNull();
        Assert.That(wall, Is.Not.Null);

        Assert.That(GetGridMoles(RelevantAtmos), Is.Zero);

        var sourceMix = SAtmos.GetTileMixture(source, true);
        Assert.That(sourceMix, Is.Not.Null);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Server.WaitRunTicks(10); // TODO test is dependant on running ticks here.

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
        });

        var mix1 = SAtmos.GetTileMixture(floor);
        Assert.That(mix1, Is.Not.Null);

        AssertMixMoles(sourceMix, mix1, Tolerance);
        AssertGridMoles(Moles, Tolerance);

        // Space the room
        var tile = new Tile(_tileDefManager[_latticeDefinition].TileId);
        _sMapSystem.SetTile(MapData.Grid, floor.ToCoordinates(), tile);

        await Server.WaitRunTicks(10);

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
            }
        });

        mix1 = SAtmos.GetTileMixture(floor);
        Assert.That(mix1, Is.Not.Null);

        AssertMixMoles(sourceMix, mix1, Tolerance);
        AssertGridMoles(0, Tolerance);
    }
}
