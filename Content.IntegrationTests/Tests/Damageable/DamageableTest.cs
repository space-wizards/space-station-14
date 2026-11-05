using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Damageable;

[TestFixture]
[TestOf(typeof(DamageableComponent))]
[TestOf(typeof(DamageableSystem))]
public sealed class DamageableTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _sDamageableSystem = default!;

    private const string TestDamageableEntityId = "TestDamageableEntityId";

    private const string TestGroup1 = "TestGroup1";
    private const string TestGroup2 = "TestGroup2";
    private const string TestGroup3 = "TestGroup3";

    private const string TestDamage1 = "TestDamage1";

    private const string TestDamage2a = "TestDamage2a";
    private const string TestDamage2b = "TestDamage2b";

    private const string TestDamage3a = "TestDamage3a";
    private const string TestDamage3b = "TestDamage3b";
    private const string TestDamage3c = "TestDamage3c";

    [TestPrototypes]
    private const string Prototypes = $@"
# Define some damage groups
- type: damageType
  id: {TestDamage1}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage2a}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage2b}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage3a}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage3b}
  name: damage-type-blunt

- type: damageType
  id: {TestDamage3c}
  name: damage-type-blunt

# Define damage Groups with 1,2,3 damage types
- type: damageGroup
  id: {TestGroup1}
  name: damage-group-brute
  damageTypes:
  - {TestDamage1}

- type: damageGroup
  id: {TestGroup2}
  name: damage-group-brute
  damageTypes:
  - {TestDamage2a}
  - {TestDamage2b}

- type: damageGroup
  id: {TestGroup3}
  name: damage-group-brute
  damageTypes:
  - {TestDamage3a}
  - {TestDamage3b}
  - {TestDamage3c}

# This container should not support TestDamage1 or TestDamage2b
- type: damageContainer
  id: testDamageContainer
  supportedGroups:
  - {TestGroup3}
  supportedTypes:
  - {TestDamage2a}

- type: entity
  id: {TestDamageableEntityId}
  name: {TestDamageableEntityId}
  components:
  - type: Damageable
  - type: Injurable
  damageContainer: testDamageContainer
";

    [Test]
    public async Task TestDamageableComponents()
    {
        var sDamageableEntity = await Spawn(TestDamageableEntityId);
        var sDamageableComponent = SComp<DamageableComponent>(sDamageableEntity);

        DamageGroupPrototype group1 = default!;
        DamageGroupPrototype group2 = default!;
        DamageGroupPrototype group3 = default!;

        DamageTypePrototype type1 = default!;
        DamageTypePrototype type2a = default!;
        DamageTypePrototype type2b = default!;
        DamageTypePrototype type3a = default!;
        DamageTypePrototype type3b = default!;
        DamageTypePrototype type3c = default!;

        FixedPoint2 typeDamage;

        await Server.WaitPost(() =>
        {
            group1 = SProtoMan.Index<DamageGroupPrototype>(TestGroup1);
            group2 = SProtoMan.Index<DamageGroupPrototype>(TestGroup2);
            group3 = SProtoMan.Index<DamageGroupPrototype>(TestGroup3);

            type1 = SProtoMan.Index<DamageTypePrototype>(TestDamage1);
            type2a = SProtoMan.Index<DamageTypePrototype>(TestDamage2a);
            type2b = SProtoMan.Index<DamageTypePrototype>(TestDamage2b);
            type3a = SProtoMan.Index<DamageTypePrototype>(TestDamage3a);
            type3b = SProtoMan.Index<DamageTypePrototype>(TestDamage3b);
            type3c = SProtoMan.Index<DamageTypePrototype>(TestDamage3c);
        });

        await Server.WaitRunTicks(5);

        await Server.WaitAssertion(() =>
        {
            var uid = sDamageableEntity;
            var ent = new Entity<DamageableComponent>(uid, sDamageableComponent);

            // Check that damage is evenly distributed over a group if its a nice multiple
            var types = group3.DamageTypes;
            var damageToDeal = FixedPoint2.New(types.Count * 5);
            DamageSpecifier damage = new(group3, damageToDeal);

            _sDamageableSystem.ChangeDamage(uid, damage, true);

            Assert.Multiple(() =>
            {
                Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(damageToDeal));
                Assert.That(_sDamageableSystem.GetDamagePerGroup(ent)[group3.ID], Is.EqualTo(damageToDeal));
                foreach (var type in types)
                {
                    Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict.TryGetValue(type, out typeDamage));
                    Assert.That(typeDamage, Is.EqualTo(damageToDeal / types.Count));
                }
            });

            // Heal
            _sDamageableSystem.ChangeDamage(uid, -damage);

            Assert.Multiple(() =>
            {
                Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));
                Assert.That(_sDamageableSystem.GetDamagePerGroup(ent)[group3.ID], Is.EqualTo(FixedPoint2.Zero));
                foreach (var type in types)
                {
                    Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict.TryGetValue(type, out typeDamage));
                    Assert.That(typeDamage, Is.EqualTo(FixedPoint2.Zero));
                }
            });

            // Check that damage works properly if it is NOT perfectly divisible among group members
            types = group3.DamageTypes;

            Assert.That(types, Has.Count.EqualTo(3));

            damage = new DamageSpecifier(group3, 14);
            _sDamageableSystem.ChangeDamage(uid, damage, true);

            Assert.Multiple(() =>
            {
                Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.New(14)));
                Assert.That(_sDamageableSystem.GetDamagePerGroup(ent)[group3.ID], Is.EqualTo(FixedPoint2.New(14)));
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict[type3a.ID], Is.EqualTo(FixedPoint2.New(4.66f)));
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict[type3b.ID], Is.EqualTo(FixedPoint2.New(4.67f)));
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict[type3c.ID], Is.EqualTo(FixedPoint2.New(4.67f)));
            });

            // Heal
            _sDamageableSystem.ChangeDamage(uid, -damage);

            Assert.Multiple(() =>
            {
                Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));
                Assert.That(_sDamageableSystem.GetDamagePerGroup(ent)[group3.ID], Is.EqualTo(FixedPoint2.Zero));
                foreach (var type in types)
                {
                    Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict.TryGetValue(type, out typeDamage));
                    Assert.That(typeDamage, Is.EqualTo(FixedPoint2.Zero));
                }

                // Test that unsupported groups return false when setting/getting damage (and don't change damage)
                Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));
            });
            damage = new DamageSpecifier(group1, FixedPoint2.New(10)) + new DamageSpecifier(type2b, FixedPoint2.New(10));
            _sDamageableSystem.ChangeDamage(uid, damage, true);

            Assert.Multiple(() =>
            {
                Assert.That(_sDamageableSystem.GetDamagePerGroup(ent).TryGetValue(group1.ID, out _), Is.False);
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict.TryGetValue(type1.ID, out typeDamage), Is.False);
                Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));
            });

            // Test SetAll and ClearAll function
            _sDamageableSystem.SetAllDamage((sDamageableEntity, sDamageableComponent), 10);
            Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.New(10 * _sDamageableSystem.GetAllDamage(uid).DamageDict.Count)));
            _sDamageableSystem.SetAllDamage((sDamageableEntity, sDamageableComponent), 0);
            Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));
            _sDamageableSystem.SetAllDamage((sDamageableEntity, sDamageableComponent), 10);
            Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.New(10 * _sDamageableSystem.GetAllDamage(uid).DamageDict.Count)));
            _sDamageableSystem.ClearAllDamage((sDamageableEntity, sDamageableComponent));
            Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));

            // Test 'wasted' healing
            _sDamageableSystem.ChangeDamage(uid, new DamageSpecifier(type3a, 5));
            _sDamageableSystem.ChangeDamage(uid, new DamageSpecifier(type3b, 7));
            _sDamageableSystem.ChangeDamage(uid, new DamageSpecifier(group3, -11));

            Assert.Multiple(() =>
            {
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict[type3a.ID], Is.EqualTo(FixedPoint2.New(1.34)));
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict[type3b.ID], Is.EqualTo(FixedPoint2.New(3.33)));
                Assert.That(_sDamageableSystem.GetAllDamage(uid).DamageDict[type3c.ID], Is.EqualTo(FixedPoint2.New(0)));
            });

            // Test Over-Healing
            _sDamageableSystem.ChangeDamage(uid, new DamageSpecifier(group3, FixedPoint2.New(-100)));
            Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));

            // Test that if no health change occurred, returns false
            _sDamageableSystem.ChangeDamage(uid, new DamageSpecifier(group3, -100));
            Assert.That(_sDamageableSystem.GetTotalDamage(ent), Is.EqualTo(FixedPoint2.Zero));
        });
    }

    // Tests healing 15 from entry 1 -> entry 2 on both damage types
    // Tests healing 10 from entry 1 -> entry 3 on only TestDamage3b
    private static TestCaseData[] _evenHealCases =
    [
        // Full heal
        new (new DamageSpecifier([TestDamage3a, TestDamage3b], 10),
            new DamageSpecifier(),
            new DamageSpecifier([TestDamage3a], 5)),
        // No full heal
        new (new DamageSpecifier([TestDamage3a, TestDamage3b], 20),
            new DamageSpecifier
             { DamageDict = new() {{TestDamage3a, 2.5},{TestDamage3b,2.5}} },
            new DamageSpecifier([TestDamage3a], 10)),
        // Partial full heal
        new (new DamageSpecifier
             { DamageDict = new() {{TestDamage3a, 5},{TestDamage3b, 20}} },
            new DamageSpecifier([TestDamage3b], 10),
            new DamageSpecifier
                { DamageDict = new() {{TestDamage3a, 5},{TestDamage3b, 10}} }),
    ];

    [Test, TestCaseSource(nameof(_evenHealCases))]
    public async Task TestEvenHeal(DamageSpecifier damaged, DamageSpecifier healed, DamageSpecifier filterHealed)
    {
        var sDamageableEntity = await Spawn(TestDamageableEntityId);
        var sDamageableComp = SComp<DamageableComponent>(sDamageableEntity);

        // Test with all damage types
        await Server.WaitPost(() =>
        {
            _sDamageableSystem.SetDamage((sDamageableEntity, sDamageableComp), damaged);
            _sDamageableSystem.HealEvenly((sDamageableEntity, sDamageableComp), -15);
            _sDamageableSystem.TrimZeros((sDamageableEntity, sDamageableComp));
        });

        Assert.That(_sDamageableSystem.GetAllDamage(sDamageableEntity), Is.EqualTo(healed));

        // Test with filtered damage types
        await Server.WaitPost(() =>
        {
            _sDamageableSystem.SetDamage((sDamageableEntity, sDamageableComp), damaged);
            _sDamageableSystem.HealEvenly((sDamageableEntity, sDamageableComp), -10, [TestDamage3b]);
            _sDamageableSystem.TrimZeros((sDamageableEntity, sDamageableComp));
        });

        Assert.That(_sDamageableSystem.GetAllDamage(sDamageableEntity), Is.EqualTo(filterHealed));
    }

    // Tests healing 15 from entry 1 -> entry 2 on both damage types
    // Tests healing 10 from entry 1 -> entry 3 on only TestDamage3b
    private static TestCaseData[] _distributedHealCases =
    [
        // Even, full heal
        new (new DamageSpecifier([TestDamage3a, TestDamage3b], 10),
            new DamageSpecifier(),
            new DamageSpecifier([TestDamage3a], 5)),
        // Even, partial heal
        new (new DamageSpecifier([TestDamage3a, TestDamage3b], 20),
            new DamageSpecifier
                { DamageDict = new() {{TestDamage3a, 2.5},{TestDamage3b,2.5}} },
            new DamageSpecifier([TestDamage3a], 10)),
        // Uneven, full heal
        new (new DamageSpecifier
                { DamageDict = new() {{TestDamage3a, 5},{TestDamage3b, 10}} },
            new DamageSpecifier(),
            new DamageSpecifier([TestDamage3a], 5)),
        // Uneven, partial heal
        new (new DamageSpecifier
                { DamageDict = new() {{TestDamage3a, 5},{TestDamage3b, 20}} },
            new DamageSpecifier
                { DamageDict = new() {{TestDamage3a, 2},{TestDamage3b, 8}} },
            new DamageSpecifier
                { DamageDict = new() {{TestDamage3a, 5},{TestDamage3b, 10}} }),
    ];

    [Test, TestCaseSource(nameof(_distributedHealCases))]
    public async Task TestDistributedHeal(DamageSpecifier damaged, DamageSpecifier healed, DamageSpecifier filterHealed)
    {
        var sDamageableEntity = await Spawn(TestDamageableEntityId);
        var sDamageableComp = SComp<DamageableComponent>(sDamageableEntity);

        // Test with all damage types
        await Server.WaitPost(() =>
        {
            _sDamageableSystem.SetDamage((sDamageableEntity, sDamageableComp), damaged);
            _sDamageableSystem.HealDistributed((sDamageableEntity, sDamageableComp), -15);
            _sDamageableSystem.TrimZeros((sDamageableEntity, sDamageableComp));
        });

        Assert.That(_sDamageableSystem.GetAllDamage(sDamageableEntity), Is.EqualTo(healed));

        // Test with filtered damage types
        await Server.WaitPost(() =>
        {
            _sDamageableSystem.SetDamage((sDamageableEntity, sDamageableComp), damaged);
            _sDamageableSystem.HealEvenly((sDamageableEntity, sDamageableComp), -10, [TestDamage3b]);
            _sDamageableSystem.TrimZeros((sDamageableEntity, sDamageableComp));
        });

        Assert.That(_sDamageableSystem.GetAllDamage(sDamageableEntity), Is.EqualTo(filterHealed));
    }
}
