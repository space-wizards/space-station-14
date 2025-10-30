using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Strip.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Strip;

public sealed class StrippableTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    /// <summary>
    /// Tests that the stripping UI is opened when drag dropping from another mob onto the player.
    /// </summary>
    [Test]
    public async Task DragDropOpensStrip()
    {
        await SpawnTarget("MobHuman");

        var userInterface = Comp<UserInterfaceComponent>(Target);
        Assert.That(userInterface.Actors, Is.Empty);

        await DragDrop(Target.Value, Player);

        Assert.That(userInterface.Actors, Is.Not.Empty);

        Assert.That(CUiSys.IsUiOpen(CTarget.Value, StrippingUiKey.Key));
        Assert.That(SUiSys.IsUiOpen(STarget.Value, StrippingUiKey.Key));
    }
}
