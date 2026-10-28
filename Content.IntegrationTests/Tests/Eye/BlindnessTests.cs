using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Eye;

/// <summary>
/// Tests for things that directly cause blindness unrelated to eye damage
/// </summary>
public sealed class BlindnessTests : InteractionTest
{
    // We need eye slots (and eyeballs) to blind/damage
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId BlindfoldPrototype = "ClothingEyesBlindfold";

    [SidedDependency(Side.Server)] private readonly SleepingSystem _sleeping = default!;

    /// <summary>
    /// Tests that equipping and removing a blindfold adds and removes blindness, respectively
    /// </summary>
    [Test]
    public async Task BlindfoldBlindnessTest()
    {
        var blindableComponent = Comp<BlindableComponent>(Player);

        Assert.That(blindableComponent.IsBlind, Is.False, "Initial blind check failed");

        // Grab blindfold, use it, and wait for the DoAfter
        var blindfold = await PlaceInHands(BlindfoldPrototype);
        await UseInHand();
        await AwaitDoAfters();

        Assert.That(blindableComponent.IsBlind, Is.True, "Blindfold did not cause blindness");

        //Now we remove the blindfold and test that IsBlind isn't set
        await Delete(blindfold);

        Assert.That(blindableComponent.IsBlind, Is.False, "Removing blindfold did not remove blindness");
    }

    /// <summary>
    /// Tests that falling asleep causes blindness
    /// </summary>
    [Test]
    public async Task SleepBlindnessTest()
    {
        var blindableComponent = Comp<BlindableComponent>(Player);

        // Sleeping inflicts blindness
        Assert.That(blindableComponent.IsBlind, Is.False, "Initial blind check failed");

        await Server.WaitPost(() =>
        {
            _sleeping.TrySleeping(SPlayer);
        });

        Assert.That(blindableComponent.IsBlind, Is.True, "Sleeping did not cause blindness");

        // Waking up removes blindness
        await Server.WaitPost(() =>
        {
            _sleeping.TryWaking(SPlayer);
        });

        Assert.That(blindableComponent.IsBlind, Is.False, "Waking up did not remove blindness");
    }
}
