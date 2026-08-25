#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Placeable;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class PlaceableDeconstruction : InteractionTest
{
    private static readonly EntProtoId Table = "Table";
    private static readonly EntProtoId TableFrame = "TableFrame";
    /// <summary>
    /// Checks that you can deconstruct placeable surfaces (i.e., placing a wrench on a table does not take priority).
    /// </summary>
    [Test]
    public async Task DeconstructTable()
    {
        await StartDeconstruction(Table);
        Assert.That(Comp<PlaceableSurfaceComponent>().IsPlaceable);
        await InteractUsing(Wrench);
        AssertPrototype(TableFrame);
        await InteractUsing(Wrench);
        AssertDeleted();
        await AssertEntityLookup((Steel, 1), (Rod, 2));
    }
}

