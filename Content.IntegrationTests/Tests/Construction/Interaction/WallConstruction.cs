#nullable enable
using Content.IntegrationTests.Tests.Interaction;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class WallConstruction : InteractionTest
{
    [Test]
    public async Task ConstructWall()
    {
        await StartConstruction(Wall);
        await InteractUsing(Steel, 2);
        Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.Null);
        ClientAssertPrototype(Girder, Target);
        await InteractUsing(Steel, 2);
        Assert.That(HandSys.GetActiveItem((SPlayer, Hands)), Is.Null);
        AssertPrototype(WallSolid);
    }

    [Test]
    public async Task DeconstructWall()
    {
        await StartDeconstruction(WallSolid);
        await InteractUsing(Weld);
        AssertPrototype(Girder);
        await Interact(Wrench, Screw);
        AssertDeleted();
        await AssertEntityLookup((Steel, 4));
    }
}
