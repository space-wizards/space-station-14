using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Eye;

/// <summary>
/// Blindfold tests
/// </summary>
public sealed class BlindfoldTest: InteractionTest
{
    // We need eye slots (and eyeballs) to blind/damage
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId BlindfoldPrototype = "ClothingEyesBlindfold";

    /// <summary>
    /// Tests that equipping and removing a blindfold adds and removes blindness, respectively
    /// </summary>
    [Test]
    public async Task BlindfoldWearRemoveTest()
    {
        var blindableComponent = Comp<BlindableComponent>(Player);

        Assert.That(blindableComponent.IsBlind, Is.False);

        // Grab blindfold, use it, and wait for the DoAfter
        var blindfold = await PlaceInHands(BlindfoldPrototype);
        await UseInHand();
        await AwaitDoAfters();

        Assert.That(blindableComponent.IsBlind, Is.True);

        //Now we remove the blindfold and test that IsBlind isn't set
        await Delete(blindfold);

        Assert.That(blindableComponent.IsBlind, Is.False);
    }
}
