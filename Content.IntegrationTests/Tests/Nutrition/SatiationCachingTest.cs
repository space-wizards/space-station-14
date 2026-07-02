using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Nutrition;

[TestFixture]
[TestOf(typeof(SatiationSystem))]
[TestOf(typeof(SatiationPrototype))]
public sealed class SatiationCachingTest : GameTest
{
    private const int MaximumValue = 100;
    private const string FirstThreshold = "first";
    private const int FirstThresholdValue = MaximumValue - 10;
    private const string SecondThreshold = "second";
    private const int SecondThresholdValue = FirstThresholdValue - 20;

    private const float FirstDecayModifier = 2.0f;
    private const float SecondDecayModifier = 3.0f;

    private const string DefinesAllSatiationId = "DefinesAllSatiationId";
    private const string DefinesFirstOnlySatiationId = "DefinesFirstOnlySatiationId";
    private const string DefinesSecondOnlySatiationId = "DefinesSecondOnlySatiationId";

    [TestPrototypes]
    private static readonly string NewSatiationProto = $@"
# Defines a value on both thresholds, so they should both get independent values
- type: satiation
  id: {DefinesAllSatiationId}
  baseDecayRate: 1
  maximumValue: {MaximumValue}
  thresholds:
    {FirstThreshold}: {FirstThresholdValue}
    {SecondThreshold}: {SecondThresholdValue}
  startingValueMinimum: 0
  startingValueMaximum: 100
  alertCategory: Hunger
  decayModifiers:
    {FirstThreshold}: {FirstDecayModifier}
    {SecondThreshold}: {SecondDecayModifier}

# Defines a value on the first threshold only, so the second should ""inherit"" it
- type: satiation
  id: {DefinesFirstOnlySatiationId}
  baseDecayRate: 1
  maximumValue: {MaximumValue}
  thresholds:
    {FirstThreshold}: {FirstThresholdValue}
    {SecondThreshold}: {SecondThresholdValue}
  startingValueMinimum: 0
  startingValueMaximum: 100
  alertCategory: Hunger
  decayModifiers:
    {FirstThreshold}: {FirstDecayModifier}
    # No second threshold

# Defines a value on the second threshold only
- type: satiation
  id: {DefinesSecondOnlySatiationId}
  baseDecayRate: 1
  maximumValue: {MaximumValue}
  thresholds:
    {FirstThreshold}: {FirstThresholdValue}
    {SecondThreshold}: {SecondThresholdValue}
  startingValueMinimum: 0
  startingValueMaximum: 100
  alertCategory: Hunger
  decayModifiers:
    {SecondThreshold}: {SecondDecayModifier}


";

    [Test]
    [RunOnSide(Side.Server)]
    public void AllThresholdsDefined() => AssertDecayModifiers(DefinesAllSatiationId,
    [
        // Above the first value, there is no threshold and we use the default value
        (MaximumValue, int.MaxValue, 1f),
        (FirstThresholdValue, FirstThresholdValue, FirstDecayModifier),
        (FirstThresholdValue - 1, FirstThresholdValue, FirstDecayModifier),
        (SecondThresholdValue, SecondThresholdValue, SecondDecayModifier),
        // No thresholds below second, so that keeps applying
        (SecondThresholdValue - 1, SecondThresholdValue, SecondDecayModifier),
        (0, SecondThresholdValue, SecondDecayModifier),
    ]);

    [Test]
    [RunOnSide(Side.Server)]
    public void FirstOnlyDefined() => AssertDecayModifiers(DefinesFirstOnlySatiationId,
    [
        // Above the first value, there is no threshold and we use the default value
        (MaximumValue, int.MaxValue, 1f),
        (FirstThresholdValue, FirstThresholdValue, FirstDecayModifier),
        (FirstThresholdValue - 1, FirstThresholdValue, FirstDecayModifier),
        (SecondThresholdValue, FirstThresholdValue, FirstDecayModifier),
        (SecondThresholdValue - 1, FirstThresholdValue, FirstDecayModifier),
        (0, FirstThresholdValue, FirstDecayModifier),
    ]);

    [Test]
    [RunOnSide(Side.Server)]
    public void SecondOnlyDefined() => AssertDecayModifiers(DefinesSecondOnlySatiationId,
    [
        // Above the first value, there is no threshold and we use the default value
        (MaximumValue, int.MaxValue, 1f),
        // There is no first threshold value defined, so we continue to "inherit" the default
        (FirstThresholdValue, int.MaxValue, 1f),
        (FirstThresholdValue - 1, int.MaxValue, 1f),
        (SecondThresholdValue, SecondThresholdValue, SecondDecayModifier),
        // No thresholds below second, so that keeps applying
        (SecondThresholdValue - 1, SecondThresholdValue, SecondDecayModifier),
        (0, SecondThresholdValue, SecondDecayModifier),
    ]);

    [SidedDependency(Side.Server)] private readonly SatiationSystem _satiation = default!;
    [SidedDependency(Side.Server)] private readonly IPrototypeManager _protoMan = default!;

    /// <summary>
    /// Verifies that the <c>expectedThreshold</c>s and <c>expectedDecayMod</c>s match the actual values retrieved from
    /// <paramref name="satiationProto"/> at the given <c>input</c> satiation value.
    /// The order of triples in <paramref name="assertions"/> does not matter.
    /// </summary>
    private void AssertDecayModifiers(
        ProtoId<SatiationPrototype> satiationProto,
        IEnumerable<(int input, int expectedThreshold, float expectedDecayMod)> assertions
    )
    {
        var proto = _protoMan.Index(satiationProto);
        using (Assert.EnterMultipleScope())
        {
            foreach (var (input, expectedThreshold, expectedDecayMod) in assertions)
            {
                _satiation.GetThresholdDataForTesting(
                    proto,
                    input,
                    out var threshold,
                    out var decayModifier,
                    out _, // Alert isn't used
                    out _ // Icon isn't used
                );

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(
                        threshold,
                        Is.EqualTo(expectedThreshold),
                        $"incorrect {nameof(threshold)} at value={input}"
                    );
                    Assert.That(
                        decayModifier,
                        Is.EqualTo(expectedDecayMod),
                        $"incorrect {nameof(decayModifier)} at value={input}"
                    );
                }
            }
        }
    }
}
