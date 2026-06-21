using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Eye;

/// <summary>
/// Tests for eye damage
/// </summary>
public sealed class EyeDamageTest: InteractionTest
{
    // We need eye slots (and eyeballs) to blind/damage
    protected override string PlayerPrototype => "MobHuman";

    [SidedDependency(Side.Server)] private readonly BlindableSystem _blindable = default!;

    [Test]
    public async Task EyeDamageBlindsTest()
    {
        var blindableComponent = Comp<BlindableComponent>(Player);
        var blinder = (SPlayer, SComp<BlindableComponent>(SPlayer));

        Assert.That(blindableComponent.IsBlind, Is.False);

        // Max eye damage inflicts blindness
        await Server.WaitPost((() =>
        {
            _blindable.AdjustEyeDamage(blinder, blindableComponent.MaxDamage);
        }));

        Assert.That(blindableComponent.IsBlind, Is.True);
    }

    [Test]
    public async Task
}
