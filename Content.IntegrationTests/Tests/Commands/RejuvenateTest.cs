#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Commands;

[TestOf(typeof(RejuvenateSystem))]
public sealed class RejuvenateTest : GameTest
{
    private static readonly ProtoId<DamageGroupPrototype> TestDamageGroup = "Toxin";

    private const string DamageableDummy = "DamageableDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {DamageableDummy}
  id: {DamageableDummy}
  components:
  - type: Damageable
  - type: Injurable
    damageContainer: Biological
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      200: Dead
";

    [SidedDependency(Side.Server)] private MobStateSystem _sMobStateSystem = null!;
    [SidedDependency(Side.Server)] private DamageableSystem _sDamageable = null!;
    [SidedDependency(Side.Server)] private RejuvenateSystem _sRejuvenate = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public async Task RejuvenateDeadTest()
    {
        var human = SSpawn(DamageableDummy);
        DamageableComponent? damageable = null;
        MobStateComponent? mobState = null;

        // Sanity check
        using (Assert.EnterMultipleScope())
        {
            Assert.That(STryComp(human, out damageable));
            Assert.That(STryComp(human, out mobState));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sMobStateSystem.IsAlive(human, mobState), Is.True);
            Assert.That(_sMobStateSystem.IsCritical(human, mobState), Is.False);
            Assert.That(_sMobStateSystem.IsDead(human, mobState), Is.False);
            Assert.That(_sMobStateSystem.IsIncapacitated(human, mobState), Is.False);
        }

        // Kill the entity
        DamageSpecifier damage = new(SProtoMan.Index(TestDamageGroup), FixedPoint2.New(10000000));

        _sDamageable.TryChangeDamage(human, damage, true);

        // Check that it is dead
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sMobStateSystem.IsAlive(human, mobState), Is.False);
            Assert.That(_sMobStateSystem.IsCritical(human, mobState), Is.False);
            Assert.That(_sMobStateSystem.IsDead(human, mobState), Is.True);
            Assert.That(_sMobStateSystem.IsIncapacitated(human, mobState), Is.True);
        }

        // Rejuvenate them
        _sRejuvenate.PerformRejuvenate(human);

        // Check that it is alive and with no damage
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sMobStateSystem.IsAlive(human, mobState), Is.True);
            Assert.That(_sMobStateSystem.IsCritical(human, mobState), Is.False);
            Assert.That(_sMobStateSystem.IsDead(human, mobState), Is.False);
            Assert.That(_sMobStateSystem.IsIncapacitated(human, mobState), Is.False);

            Assert.That(_sDamageable.GetTotalDamage((human, damageable)), Is.EqualTo(FixedPoint2.Zero));
        }
    }
}
