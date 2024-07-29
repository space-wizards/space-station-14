using Content.IntegrationTests.Tests.Interaction;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Tiles;

public sealed class TileConstructionTests : InteractionTest
{
    /// <summary>
    /// Test placing and cutting a single lattice.
    /// </summary>
    [Test]
    public async Task PlaceThenCutLattice()
    {
        await AssertTile(Plating);
        await AssertTile(Plating, PlayerCoords);
        AssertGridCount(1);
        await SetTile(null);
        await InteractUsing(Rod);
        await AssertTile(Lattice);
        Assert.That(Hands.ActiveHandEntity, Is.Null);
        await InteractUsing(Cut);
        await AssertTile(null);
        await AssertEntityLookup((Rod, 1));
        AssertGridCount(1);
    }

    /// <summary>
    /// Test placing and cutting a single lattice in space (not adjacent to any existing grid.
    /// </summary>
    [Test]
    public async Task CutThenPlaceLatticeNewGrid()
    {
        await AssertTile(Plating);
        await AssertTile(Plating, PlayerCoords);
        AssertGridCount(1);

        // Remove grid
        await SetTile(null);
        await SetTile(null, PlayerCoords);
        Assert.That(MapData.Grid.Comp.Deleted);
        AssertGridCount(0);

        // Place Lattice
        var oldPos = TargetCoords;
        TargetCoords = SEntMan.GetNetCoordinates(new EntityCoordinates(MapData.MapUid, 1, 0));
        await InteractUsing(Rod);
        TargetCoords = oldPos;
        await AssertTile(Lattice);
        AssertGridCount(1);

        // Cut lattice
        Assert.That(Hands.ActiveHandEntity, Is.Null);
        await InteractUsing(Cut);
        await AssertTile(null);
        AssertGridCount(0);

        await AssertEntityLookup((Rod, 1));
    }

    /// <summary>
    /// Test space -> floor -> plating
    /// </summary>
    [Test]
    public async Task FloorConstructDeconstruct()
    {
        await AssertTile(Plating);
        await AssertTile(Plating, PlayerCoords);
        AssertGridCount(1);

        // Remove grid
        await SetTile(null);
        await SetTile(null, PlayerCoords);
        Assert.That(MapData.Grid.Comp.Deleted);
        AssertGridCount(0);

        // Space -> Lattice
        var oldPos = TargetCoords;
        TargetCoords = SEntMan.GetNetCoordinates(new EntityCoordinates(MapData.MapUid, 1, 0));
        await InteractUsing(Rod);
        TargetCoords = oldPos;
        await AssertTile(Lattice);
        AssertGridCount(1);

        // Lattice -> Plating
        await InteractUsing(Steel);
        Assert.That(Hands.ActiveHandEntity, Is.Null);
        await AssertTile(Plating);
        AssertGridCount(1);

        // Plating -> Tile
        await InteractUsing(FloorItem);
        Assert.That(Hands.ActiveHandEntity, Is.Null);
        await AssertTile(Floor);
        AssertGridCount(1);

        // Tile -> Plating
        await InteractUsing(Pry);
        await AssertTile(Plating);
        AssertGridCount(1);

        await AssertEntityLookup((FloorItem, 1));
    }
}
