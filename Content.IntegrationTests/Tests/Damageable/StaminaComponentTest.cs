#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests.Damageable;

public sealed class StaminaComponentTest : GameTest
{
    private static readonly string[] EntitiesWithStamina = GameDataScrounger.EntitiesWithComponent("Stamina");

    [Test]
    [TestOf(typeof(StaminaComponent))]
    [TestCaseSource(nameof(EntitiesWithStamina))]
    [Description($"Ensures every entity with {nameof(StaminaComponent)} has a valid stamina configuration.")]
    [RunOnSide(Side.Server)]
    public async Task ValidateStamina(string protoKey)
    {
        var proto = SProtoMan.Index(protoKey);
        proto.TryGetComponent<StaminaComponent>(out var comp, SEntMan.ComponentFactory);
        Assert.That(comp, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comp.AnimationThreshold,
                Is.LessThan(comp.CritThreshold),
                $"Animation threshold on {proto.ID} must be less than its crit threshold.");

            // TODO(Kaylie): "value is in range" serializer. Needs some serializationmanager improvements.
            Assert.That(comp.Decay, Is.Positive.Or.Zero, "Negative decay results in nonsensical behavior.");
            Assert.That(comp.Cooldown, Is.Positive.Or.Zero, "Negative cooldown results in nonsensical behavior");
            Assert.That(comp.BaseCritThreshold, Is.Positive);
            Assert.That(comp.CritThreshold, Is.Positive);
            Assert.That(comp.AfterCritDecayMultiplier, Is.Positive);
            Assert.That(comp.ForceStandStamina, Is.Positive);

            // NUnit's analyzer is defective here. Cool.
#pragma warning disable NUnit2041
            Assert.That(comp.StunModifierThresholds.Keys,
                Has.All.GreaterThanOrEqualTo(FixedPoint2.Zero).And.LessThanOrEqualTo(FixedPoint2.New(1.0f)),
                "The stun thresholds are percentages and should be in the [0, 1.0] range.");
#pragma warning restore NUnit2041
        }
    }
}
