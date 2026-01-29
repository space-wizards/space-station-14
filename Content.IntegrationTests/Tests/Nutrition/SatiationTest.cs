using System.Collections.Generic;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Nutrition;

[TestFixture]
[TestOf(typeof(SatiationSystem))]
[TestOf(typeof(SatiationPrototype))]
public sealed class SatiationTest
{
    [Test]
    public async Task BasicTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var sys = entMan.System<SatiationSystem>();
            var ent = entMan.Spawn(TestProto, MapCoordinates.Nullspace);
            var entity = new Entity<SatiationComponent>(ent, server.EntMan.GetComponent<SatiationComponent>(ent));

            Assert.Multiple(() =>
            {
                Assert.That(sys.GetValueOrNull(entity, SatType),
                    Is.LessThanOrEqualTo(StartingMax).And.GreaterThanOrEqualTo(StartingMin));
                Assert.That(sys.IsValueInRange(entity, SatType, above: StartingMin, below: StartingMax),
                    Is.True);
            });

            sys.SetValue(entity, SatType, MiddleKey);
            Assert.That(sys.GetValueOrNull(entity, SatType), Is.EqualTo(MiddleValue));

            sys.ModifyValue(entity, SatType, -20);
            Assert.That(sys.GetValueOrNull(entity, SatType), Is.EqualTo(MiddleValue - 20));

            sys.ModifyValue(entity, SatType, -int.MaxValue);
            Assert.That(sys.GetValueOrNull(entity, SatType), Is.EqualTo(0));

            sys.ModifyValue(entity, SatType, int.MaxValue);
            Assert.Multiple(() =>
            {
                Assert.That(sys.GetValueOrNull(entity, SatType), Is.EqualTo(MaxValue));
                Assert.That(sys.GetValueOrNull(entity, SatType + NotRealKey), Is.Null);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GetValueByThresholdTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var sys = entMan.System<SatiationSystem>();
            var ent = entMan.Spawn(TestProto, MapCoordinates.Nullspace);
            var entity = new Entity<SatiationComponent>(ent, server.EntMan.GetComponent<SatiationComponent>(ent));
            var dict = new Dictionary<int, int>
            {
                // Arbitrary order to test that the implementation doesn't care about order.
                [20] = 20,
                [0] = 0,
                [40] = 40,
                [80] = 80,
                [100] = 100,
                [60] = 60,
            };

            sys.SetValue(entity, SatType, value: 100);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(100));
            });

            sys.SetValue(entity, SatType, value: 55);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(60));
            });

            sys.SetValue(entity, SatType, value: 0);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(0));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GetValueByThresholdKeysTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var sys = entMan.System<SatiationSystem>();
            var ent = entMan.Spawn(TestProto, MapCoordinates.Nullspace);
            var entity = new Entity<SatiationComponent>(ent, server.EntMan.GetComponent<SatiationComponent>(ent));
            var dict = new Dictionary<string, int>
            {
                // Arbitrary order to test that the implementation doesn't care about order.
                [DeadKey] = 20,
                [MaxxedKey] = 0,
                [MiddleKey] = 40,
            };

            sys.SetValue(entity, SatType, MaxxedKey);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(0));
            });

            sys.ModifyValue(entity, SatType, -10);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(0));
            });

            sys.SetValue(entity, SatType, MiddleKey);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(40));
            });

            sys.SetValue(entity, SatType, DeadKey);
            Assert.Multiple(() =>
            {
                var res = sys.TryGetValueByThreshold(entity, SatType, dict, out var result);
                Assert.That(res, Is.True);
                Assert.That(result, Is.EqualTo(20));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RangeTests()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var sys = entMan.System<SatiationSystem>();
            var ent = entMan.Spawn(TestProto, MapCoordinates.Nullspace);
            var entity = new Entity<SatiationComponent>(ent, server.EntMan.GetComponent<SatiationComponent>(ent));

            sys.SetValue(entity, SatType, value: 100);
            Assert.Multiple(() =>
            {
                Assert.That(sys.IsValueInRange(entity, SatType, above: DeadKey), Is.True);
                Assert.That(sys.IsValueInRange(entity, SatType, above: MaxxedKey), Is.False);
                Assert.That(sys.IsValueInRange(entity, SatType, below: MaxxedKey, hypotheticalValueDelta: -1), Is.True);
            });

            sys.SetValue(entity, SatType, value: MiddleValue + 5);
            Assert.Multiple(() =>
            {
                Assert.That(sys.IsValueInRange(entity, SatType, above: MiddleKey), Is.True);
                Assert.That(sys.IsValueInRange(entity, SatType, above: MiddleKey, below: MaxxedKey),
                    Is.True);
                Assert.That(
                    sys.IsValueInRange(entity, SatType, above: MaxxedKey, hypotheticalValueDelta: -10),
                    Is.False);
            });

            Assert.Multiple(() =>
            {
                // I cannot be bothered to make these into constants.
#pragma warning disable RA0033
                Assert.That(sys.IsValueInRange(entity, SatType, above: 0), Is.True);
                Assert.That(sys.IsValueInRange(entity, SatType, above: -1000), Is.True);
                Assert.That(sys.IsValueInRange(entity, SatType, above: 100), Is.False);
                Assert.That(() => sys.IsValueInRange(entity, SatType, below: 60, above: 70),
#if DEBUG
                    Throws.InstanceOf<DebugAssertException>()
#else
                        Is.False
#endif
                );
                Assert.That(sys.IsValueInRange(entity, SatType, below: 50, hypotheticalValueDelta: -10),
                    Is.True);
#pragma warning restore RA0033
            });

            sys.SetValue(entity, SatType, value: 0);
            Assert.Multiple(() =>
            {
                Assert.That(sys.IsValueInRange(entity, SatType, above: NotRealKey), Is.False);
                Assert.That(() => sys.IsValueInRange(entity, SatType),
#if DEBUG
                    Throws.InstanceOf<DebugAssertException>()
#else
                        Is.True
#endif
                );
            });
        });

        await pair.CleanReturnAsync();
    }

    private const string TestSatiationId = "TestSatiation";
    private const string DeadKey = "Dead";
    private const string MiddleKey = "Okay";
    private const string MaxxedKey = "Maxxed";
    private const string NotRealKey = "ashfdjkashfljkahdjskfjadshfgkjlhadsekljfhjalds";
    private static readonly ProtoId<SatiationTypePrototype> SatType = "Hunger";
    private const string TestProto = "TestSatiationDummy";
    private const int StartingMin = 30;
    private const int StartingMax = 35;
    private const int MiddleValue = 50;
    private const int MaxValue = 100;

    [TestPrototypes]
    private static readonly string SatiationPrototypes =
        $@"
- type: satiation
  id: {TestSatiationId}
  baseDecayRate: 1
  maximumValue: {MaxValue}
  keys: # Intentionally out of ordinal order.
    {DeadKey}: 0
    {MaxxedKey}: 100
    {MiddleKey}: {MiddleValue}
  startingValueMinimum: {StartingMin}
  startingValueMaximum: {StartingMax}
  decayModifiers:
    25: 0.5
  speedModifiers:
    50: 0.8
  damages:
    10: null
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
}
