using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using NUnit.Framework.Constraints;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Nutrition;

[TestFixture]
[TestOf(typeof(SatiationSystem))]
[TestOf(typeof(SatiationPrototype))]
public sealed class SatiationTest : GameTest
{
    private const string TestSatiationId = "TestSatiation";
    private const string DeadKey = "Dead";
    private const string MiddleKey = "Okay";
    private const string MaxxedKey = "Maxxed";
    private const string NotRealKey = "ashfdjkashfljkahdjskfjadshfgkjlhadsekljfhjalds";
    private static readonly ProtoId<SatiationTypePrototype> TestSatiationType = "Hunger";
    private const string TestProto = "TestSatiationDummy";
    private const int StartingMin = 30;
    private const int StartingMax = 35;
    private const int MiddleValue = 50;
    private const int MaxValue = 100;

    [TestPrototypes]
    private static readonly string SatiationPrototypes = $@"
- type: satiation
  id: {TestSatiationId}
  baseChangeRate: -1
  maximumValue: {MaxValue}
  thresholds: # Intentionally out of ordinal order.
    {DeadKey}: 0
    {MaxxedKey}: 100
    {MiddleKey}: {MiddleValue}
  startingValueMinimum: {StartingMin}
  startingValueMaximum: {StartingMax}
  changeModifiers:
    25: 0.5
  alertCategory: Hunger

- type: entity
  id: {TestProto}
  name: dummy
  components:
  - type: Satiation
    satiations:
      Hunger:
        prototype: {TestSatiationId}
";

    [SidedDependency(Side.Server)] private readonly SatiationSystem _satiation = default!;

    [Test, RunOnSide(Side.Server)]
    [Description(
        "Verifies the basic operations of 'SatiationSystem.SetValue', 'SatiationSystem.ModifyValue', and 'SatiationSystem.GetValueOrNull'")]
    public void SatiationBasicTest()
    {
        var entity = SEntity<SatiationComponent>(SSpawn(TestProto));

        // Verify the starting value is in the starting range.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_satiation.GetValueOrNull(entity, TestSatiationType),
                Is.LessThanOrEqualTo(StartingMax).And.GreaterThanOrEqualTo(StartingMin));
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: StartingMin, below: StartingMax),
                Is.True);
        }

        // The rest of this modifies the value and verifies the numeric value is what's expected

        _satiation.SetValue(entity, TestSatiationType, MiddleKey);
        Assert.That(_satiation.GetValueOrNull(entity, TestSatiationType), Is.EqualTo(MiddleValue));

        _satiation.ModifyValue(entity, TestSatiationType, -20);
        Assert.That(_satiation.GetValueOrNull(entity, TestSatiationType), Is.EqualTo(MiddleValue - 20));

        _satiation.ModifyValue(entity, TestSatiationType, -int.MaxValue);
        Assert.That(_satiation.GetValueOrNull(entity, TestSatiationType), Is.Zero);

        _satiation.ModifyValue(entity, TestSatiationType, int.MaxValue);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_satiation.GetValueOrNull(entity, TestSatiationType), Is.EqualTo(MaxValue));
            Assert.That(_satiation.GetValueOrNull(entity, TestSatiationType + NotRealKey), Is.Null);
        }
    }

    [Test, RunOnSide(Side.Server)]
    [Description("Verifies 'SatiationSystem.TryGetValueByThreshold' when threshold keys are integers")]
    public void SatiationGetValueByThresholdTest()
    {
        var entity = SEntity<SatiationComponent>(SSpawn(TestProto));
        var dict = new Dictionary<SatiationValue, int>
        {
            // Arbitrary order to test that the implementation doesn't care about order.
            [20] = 20,
            [0] = 0,
            [40] = 40,
            [80] = 80,
            [100] = 100,
            [60] = 60,
        };

        // All of these work by setting a value, calling `TryGetValueByThreshold`, and verifying the exact results are
        // what's expected.

        _satiation.SetValue(entity, TestSatiationType, value: 100);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.EqualTo(100));
            Assert.That(nextHigher, Is.EqualTo(100));
            Assert.That(nextLower, Is.EqualTo(80));
        }

        _satiation.SetValue(entity, TestSatiationType, value: 55);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.EqualTo(60));
            Assert.That(nextHigher, Is.EqualTo(60));
            Assert.That(nextLower, Is.EqualTo(40));
        }

        _satiation.SetValue(entity, TestSatiationType, value: 0);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.Zero);
            Assert.That(nextLower, Is.Null);
        }
    }

    [Test, RunOnSide(Side.Server)]
    [Description("Verifies 'SatiationSystem.TryGetValueByThreshold' when threshold keys are strings")]
    public void SatiationGetValueByThresholdKeysTest()
    {
        var entity = SEntity<SatiationComponent>(SSpawn(TestProto));
        var dict = new Dictionary<SatiationValue, int>
        {
            // Arbitrary order to test that the implementation doesn't care about order.
            [DeadKey] = 20,
            [MaxxedKey] = 0,
            [MiddleKey] = 40,
        };

        // All of these work by setting a value, calling `TryGetValueByThreshold`, and verifying the exact results are
        // what's expected.

        _satiation.SetValue(entity, TestSatiationType, MaxxedKey);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.Zero);
            Assert.That(nextHigher, Is.EqualTo(MaxValue));
            Assert.That(nextLower, Is.EqualTo(MiddleValue));
        }

        _satiation.ModifyValue(entity, TestSatiationType, -10);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.Zero);
            Assert.That(nextHigher, Is.EqualTo(MaxValue));
            Assert.That(nextLower, Is.EqualTo(MiddleValue));
        }

        _satiation.SetValue(entity, TestSatiationType, MiddleKey);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.EqualTo(40));
            Assert.That(nextHigher, Is.EqualTo(MiddleValue));
            Assert.That(nextLower, Is.Zero);
        }

        _satiation.SetValue(entity, TestSatiationType, DeadKey);
        using (Assert.EnterMultipleScope())
        {
            var res = _satiation.TryGetValueByThreshold(entity,
                TestSatiationType,
                dict,
                out var result,
                out var nextHigher,
                out var nextLower);
            Assert.That(res, Is.True);
            Assert.That(result, Is.EqualTo(20));
            Assert.That(nextHigher, Is.Zero);
            Assert.That(nextLower, Is.Null);
        }
    }

    [Test, RunOnSide(Side.Server)]
    [Description("Verifies 'SatiationSystem.IsValueInRange'")]
    [SuppressMessage("Assertion",
        "NUnit2057:Remove unnecessary lambda expression",
        Justification = "Necessity of lambda depends on build configuration")]
    public void SatiationRangeTests()
    {
        var entity = SEntity<SatiationComponent>(SSpawn(TestProto));

        // All of these work by setting a value, then asserting that `IsValueInRange` for various ranges returns what's
        // expected.

        _satiation.SetValue(entity, TestSatiationType, value: 100);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: DeadKey), Is.True);
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: MaxxedKey), Is.False);
            Assert.That(_satiation.IsValueInRange(entity,
                    TestSatiationType,
                    below: MaxxedKey,
                    hypotheticalValueDelta: -1),
                Is.True);
        }

        _satiation.SetValue(entity, TestSatiationType, value: MiddleValue + 5);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: MiddleKey), Is.True);
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: MiddleKey, below: MaxxedKey),
                Is.True);
            Assert.That(
                _satiation.IsValueInRange(entity, TestSatiationType, above: MaxxedKey, hypotheticalValueDelta: -10),
                Is.False);
        }

        using (Assert.EnterMultipleScope())
        {
            // Disable "ForbidLiteral" errors. Making these all into constants to be used once is unnecessary.
#pragma warning disable RA0033
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: 0), Is.True);
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: -1000), Is.True);
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: 100), Is.False);
            Assert.That(() => _satiation.IsValueInRange(entity, TestSatiationType, below: 60, above: 70),
                Is.False.OrFailsDebugAssertInDebug());
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, below: 50, hypotheticalValueDelta: -10),
                Is.True);
#pragma warning restore RA0033
        }

        _satiation.SetValue(entity, TestSatiationType, value: 0);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_satiation.IsValueInRange(entity, TestSatiationType, above: NotRealKey), Is.False);
            Assert.That(() => _satiation.IsValueInRange(entity, TestSatiationType),
                Is.True.OrFailsDebugAssertInDebug());
        }
    }
}

static file class ConstraintExt
{
#if DEBUG
    private const bool IsDebug = true;
#else
    private const bool IsDebug = false;
#endif

    extension(Constraint constraint)
    {
        /// <summary>
        /// Returns the receiver <see cref="Constraint"/> when <c>#define DEBUG</c> is false, otherwise returns a
        /// <see cref="ThrowsConstraint"/> with exception type <see cref="DebugAssertException"/>. This is useful when
        /// something throws in Debug, but fails gracefully otherwise.
        /// </summary>
        // TODO Put this somewhere else reusable https://github.com/space-wizards/space-station-14/pull/34166#discussion_r3692882923
        public Constraint OrFailsDebugAssertInDebug() =>
#pragma warning disable CS0162 // Unreachable code detected
            // ReSharper disable once HeuristicUnreachableCode
            IsDebug ? Throws.InstanceOf<DebugAssertException>() : constraint;
#pragma warning restore CS0162 // Unreachable code detected
    }
}
