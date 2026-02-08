using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Nutrition;

[TestFixture]
[TestOf(typeof(SatiationSystem))]
[TestOf(typeof(SatiationPrototype))]
public sealed class SatiationCachingTest
{
    private const string NewSatiationId = "NewSatiationId";
    private const int MaximumValue = 100;
    private const string FirstThreshold = "first";
    private const int FirstThresholdValue = MaximumValue - 20;
    private const string SecondThreshold = "second";
    private const int SecondThresholdValue = FirstThresholdValue - 20;

    private const float FirstDecayModifier = 1.0f;
    private const float SecondDecayModifier = 2.0f;

    private const float FirstSpeedModifier = 3.0f;

    [TestPrototypes]
    private static readonly string NewSatiationProto = $@"
- type: satiation
  id: {NewSatiationId}
  baseDecayRate: 1
  maximumValue: {MaximumValue}
  keys:
    {FirstThreshold}: {FirstThresholdValue}
    {SecondThreshold}: {SecondThresholdValue}
  startingValueMinimum: 0
  startingValueMaximum: 100
  alertCategory: Hunger
  decayModifiers: # Both define this, so they should each get their own values.
    {FirstThreshold}: {FirstDecayModifier}
    {SecondThreshold}: {SecondDecayModifier}
  speedModifiers: # First defines this, so both should use it.
    {FirstThreshold}: {FirstSpeedModifier}
  damages: # First defines this, but then second 'undefines' it, so only first should use it.
    {FirstThreshold}:
      types:
        Caustic: 4
    {SecondThreshold}: null
  # Alerts aren't used.
  icons: {{}} # Icons 'inherit' the lack of value from the top default threshold.
";

    [Test]
    public async Task ThresholdDataCachingTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var sys = entMan.System<SatiationSystem>();
            var proto = protoMan.Index<SatiationPrototype>(NewSatiationId);

            // Verify the values of the implicit top threshold which uses defaults for everything.
            GetAndAssertValues(
                sys,
                proto,
                MaximumValue,
                expectedThreshold: int.MaxValue,
                expectedDecayMod: 1f,
                expectedSpeedMod: 1f,
                expectDamageSpec: false
            );

            // First threshold
            GetAndAssertValues(
                sys,
                proto,
                FirstThresholdValue - 1,
                expectedThreshold: FirstThresholdValue,
                expectedDecayMod: FirstDecayModifier,
                expectedSpeedMod: FirstSpeedModifier,
                expectDamageSpec: true
            );

            // Second threshold
            GetAndAssertValues(
                sys,
                proto,
                SecondThresholdValue - 1,
                expectedThreshold: SecondThresholdValue,
                expectedDecayMod: SecondDecayModifier,
                expectedSpeedMod: FirstSpeedModifier, // The second threshold "inherits" this value from the first.
                expectDamageSpec: false
            );
        });

        await pair.CleanReturnAsync();
        return;

        void GetAndAssertValues(
            SatiationSystem sys,
            SatiationPrototype proto,
            int satiationValue,
            int expectedThreshold,
            float expectedDecayMod,
            float expectedSpeedMod,
            bool expectDamageSpec
        )
        {
            // I am the test, this is for me.
#pragma warning disable CS0618 // Type or member is obsolete
            sys.GetThresholdDataForTesting(
                proto,
                satiationValue,
                out var threshold,
                out var decayModifier,
                out var speedModifier,
                out var damage,
                out _, // Alert isn't used
                out var icon
            );
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Multiple(() =>
            {
                Assert.That(threshold, Is.EqualTo(expectedThreshold));
                Assert.That(decayModifier, Is.EqualTo(expectedDecayMod));
                Assert.That(speedModifier, Is.EqualTo(expectedSpeedMod));
                Assert.That(damage, expectDamageSpec ? Is.Not.Null : Is.Null);
                Assert.That(icon, Is.Null); // Icon is expected to be implicitly null always.
            });
        }
    }
}
