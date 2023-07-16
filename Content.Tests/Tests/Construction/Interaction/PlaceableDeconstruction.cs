using Content.Shared.Placeable;
using Content.Tests.Tests.Interaction;

namespace Content.Tests.Tests.Construction.Interaction;

public sealed class PlaceableDeconstruction : InteractionTest
{
    /// <summary>
    /// Checks that you can deconstruct placeable surfaces (i.e., placing a wrench on a table does not take priority).
    /// </summary>
    [Test]
    public async Task DeconstructTable()
    {
        await StartDeconstruction("Table");
        Assert.That(Comp<PlaceableSurfaceComponent>().IsPlaceable);
        await Interact(Wrench);
        AssertPrototype("TableFrame");
        await Interact(Wrench);
        AssertDeleted();
        await AssertEntityLookup((Steel, 1), (Rod, 2));
    }
}

