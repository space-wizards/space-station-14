using System.Linq;
using Content.IntegrationTests.Utility;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests.Damageable;

public sealed class StaminaComponentTest
{
    private static string[] _entitiesWithStamina = GameDataScrounger.EntitiesWithComponent("Stamina");

    [Test]
    [TestOf(typeof(StaminaComponent))]
    [TestCaseSource(nameof(_entitiesWithStamina))]
    [Description("Ensures every entity with Stamina has a valid stamina configuration.")]
    public async Task ValidateStamina(string protoKey)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;

        await server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                var proto = protoMan.Index(protoKey);
                var comp = (StaminaComponent)proto.Components["Stamina"].Component;

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
        });

        await pair.CleanReturnAsync();
    }
}
