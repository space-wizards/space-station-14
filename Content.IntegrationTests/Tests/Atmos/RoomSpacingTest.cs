using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

public sealed class RoomSpacingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_room.yml");

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

        var lookup = LookupSystem.GetEntitiesIntersecting(wallPos);
        var wall = lookup.FirstOrNull();
        Assert.That(wall, Is.Not.Null);

        Assert.That(GetGridMoles(RelevantAtmos), Is.EqualTo(0));

        var sourceMix = SAtmos.GetTileMixture(source, true);
        Assert.That(sourceMix, Is.Not.EqualTo(null));
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Server.WaitRunTicks(500);

        var mix1 = SAtmos.GetTileMixture(floor);
        Assert.That(mix1, Is.Not.EqualTo(null));

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

        var lookup = LookupSystem.GetEntitiesIntersecting(wallPos);
        var wall = lookup.FirstOrNull();
        Assert.That(wall, Is.Not.Null);

        Assert.That(GetGridMoles(RelevantAtmos), Is.EqualTo(0));

        var sourceMix = SAtmos.GetTileMixture(source, true);
        Assert.That(sourceMix, Is.Not.EqualTo(null));
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
        });

        var mix1 = SAtmos.GetTileMixture(floor);
        Assert.That(mix1, Is.Not.EqualTo(null));

        AssertMixMoles(sourceMix, mix1, Tolerance);
        AssertGridMoles(Moles, Tolerance);

        // Space the room
        await SetTile(Lattice, SEntMan.GetNetCoordinates(floor.ToCoordinates()), MapData.Grid);

        await Server.WaitRunTicks(10);

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);
            }
        });

        mix1 = SAtmos.GetTileMixture(floor);
        Assert.That(mix1, Is.Not.EqualTo(null));

        AssertMixMoles(sourceMix, mix1, Tolerance);
        AssertGridMoles(0, Tolerance);
    }
}
