using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Eye;

/// <summary>
/// Tests for eye damage
/// </summary>
public sealed class EyeDamageTests : InteractionTest
{
    // We need eye slots (and eyeballs) to blind/damage
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId LockerPrototype = "LockerFreezer";
    private static readonly EntProtoId WeldingMaskPrototype = "ClothingHeadHatWelding";
    private const int ExpectedWelderEyeDamage = 1; // 1 is a magic number in EyeProtectionSystem.cs. Test will fail if this is changed at some point.

    [SidedDependency(Side.Server)] private readonly BlindableSystem _blindable = default!;

    /// <summary>
    /// Tests that applying maximum eye damage triggers blindness.
    /// </summary>
    [Test]
    public async Task EyeDamageBlindsTest()
    {
        var blindableComponent = Comp<BlindableComponent>(Player);

        Assert.That(blindableComponent.IsBlind, Is.False, "Initial blind check failed");

        // Max eye damage inflicts blindness
        await Server.WaitPost(() =>
        {
            _blindable.AdjustEyeDamage(SPlayer, blindableComponent.MaxDamage);
        });

        Assert.That(blindableComponent.IsBlind, Is.True, "Max eye damage did not inflict blindness");
    }

    /// <summary>
    /// Tests that welding without protection causes eye damage, and welding with protection does not.
    /// </summary>
    [Test]
    public async Task WelderCausesEyeDamageTest()
    {
        var blindableComponent = Comp<BlindableComponent>(Player);
        await SpawnTarget(LockerPrototype);

        // Welding without protection causes eye damage
        Assert.That(blindableComponent.EyeDamage, Is.Zero, "Initial eye damage >0");

        await InteractUsing(Weld);

        Assert.That(blindableComponent.EyeDamage, Is.EqualTo(ExpectedWelderEyeDamage), "Unexpected eye damage amount");

        // Welding with protection prevents eye damage
        var initialEyeDamage = blindableComponent.EyeDamage;

        await PlaceInHands(WeldingMaskPrototype);
        await UseInHand();
        await AwaitDoAfters();
        await InteractUsing(Weld);

        Assert.That(blindableComponent.EyeDamage, Is.EqualTo(initialEyeDamage), "Eye damage inflicted despite protection");
    }
}
