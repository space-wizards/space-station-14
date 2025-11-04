using Content.Shared.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

public sealed class RoomSpacingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_room.yml");

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
        var wall = lookup.FirstOrNull().Value;
        Assert.That(wall, Is.Not.EqualTo(null));

        Assert.That(GetGridMoles(RelevantAtmos), Is.EqualTo(0));

        var sourceMix = SAtmos.GetTileMixture(source, true);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Pair.RunTicksSync(500);

        var mix1 = SAtmos.GetTileMixture(floor);

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(sourceMix.TotalMoles, mix1.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            var moles = GetGridMoles(RelevantAtmos);
            Assert.That(MathHelper.CloseToPercent(moles, Moles, Tolerance));
        });

        // Space the room
        await Pair.Server.WaitAssertion(() =>
        {
            SEntMan.DeleteEntity(wall);
        });

        await Pair.RunTicksSync(1000);

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(sourceMix.TotalMoles, mix1.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            var moles = GetGridMoles(RelevantAtmos);
            Assert.That(MathHelper.CloseToPercent(moles, 0, Tolerance));
        });
    }

    [Test]
    public async Task PryLattice()
    {
        var markers = SEntMan.AllEntities<TestMarkerComponent>();

        EntityUid source, floor, wallPos;
        source = floor = wallPos = EntityUid.Invalid;

        Vector2i sourceXY, floorXY, wallXY;
        sourceXY = floorXY = wallXY = Vector2i.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(GetMarker(markers, "source", out source));
            Assert.That(GetMarker(markers, "floor", out floor));
            Assert.That(GetMarker(markers, "wall", out wallPos));
        });

        var lookup = LookupSystem.GetEntitiesIntersecting(wallPos);
        var wall = lookup.FirstOrNull().Value;
        Assert.That(wall, Is.Not.EqualTo(null));

        Assert.That(GetGridMoles(RelevantAtmos), Is.EqualTo(0));

        var sourceMix = SAtmos.GetTileMixture(source, true);
        sourceMix.AdjustMoles(Gas.Frezon, Moles);

        await Pair.RunTicksSync(500);

        var mix1 = SAtmos.GetTileMixture(floor);

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(sourceMix.TotalMoles, mix1.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            var moles = GetGridMoles(RelevantAtmos);
            Assert.That(MathHelper.CloseToPercent(moles, Moles, Tolerance));
        });

        // Space the room
        await SetTile(Lattice, SEntMan.GetNetCoordinates(floor.ToCoordinates()), MapData.Grid);

        await Pair.RunTicksSync(1000);

        mix1 = SAtmos.GetTileMixture(floor);

        Assert.Multiple(() =>
        {
            // Check if pressure has more or less evenly distributed
            Assert.That(MathHelper.CloseToPercent(sourceMix.TotalMoles, mix1.TotalMoles, Tolerance));

            // Make sure we're not creating/destroying matter
            var moles = GetGridMoles(RelevantAtmos);
            Assert.That(MathHelper.CloseToPercent(moles, 0, Tolerance));
        });
    }
}
