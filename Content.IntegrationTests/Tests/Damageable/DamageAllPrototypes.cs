using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests.Damageable;

[TestFixture]
[TestOf(typeof(DamageableComponent))]
[TestOf(typeof(DamageableSystem))]
public sealed class DamageAllPrototypesTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damageableSystem = default!;

    [Test]
    [TestOf(typeof(DamageableSystem))]
    [Description("Ensures all Entity Prototypes with damageable can be damaged.")]
    public async Task TestDamageableComponents()
    {
        var map = await Pair.CreateTestMap();

        try
        {
            foreach (var damageable in GameDataScrounger.EntitiesWithComponent("Damageable"))
            {
                var entity = await SpawnAtPosition(damageable, map.GridCoords);

                try
                {
                    // Intentionally cannot take damage, ignore it.
                    if (SEntMan.HasComponent<GodmodeComponent>(entity))
                        continue;

                    var canBeDamaged = false;

                    foreach (var type in SProtoMan.EnumeratePrototypes<DamageTypePrototype>())
                    {
                        if (!_damageableSystem.CanBeDamagedBy(entity, type))
                            continue;

                        canBeDamaged = true;

                        await Server.WaitAssertion(() =>
                        {
                            var damage = new DamageSpecifier(type, FixedPoint2.Epsilon);
                            var previousDamage = _damageableSystem.GetTotalDamage(entity);
                            _damageableSystem.ChangeDamage(entity, damage, ignoreResistances: true);
                            Assert.That(
                                _damageableSystem.GetTotalDamage(entity),
                                Is.EqualTo(FixedPoint2.Epsilon + previousDamage),
                                $"{damageable} should take {type.ID} damage.");

                            _damageableSystem.ClearAllDamage(entity);
                        });
                    }

                    // Ensure that this entity can actually be damaged.
                    Assert.That(canBeDamaged, Is.True, $"{damageable} cannot be damaged by any damage type.");
                }
                finally
                {
                    await Server.WaitPost(() => SEntMan.DeleteEntity(entity));
                }
            }
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.DeleteEntity(map.MapUid));
        }
    }
}
