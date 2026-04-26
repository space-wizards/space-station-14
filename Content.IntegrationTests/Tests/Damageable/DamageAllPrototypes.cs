using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Damageable;

[TestFixture]
[TestOf(typeof(DamageableComponent))]
[TestOf(typeof(DamageableSystem))]
public sealed class DamageAllPrototypesTest : GameTest
{
    private static string[] _damageables = GameDataScrounger.EntitiesWithComponent("Damageable");

    [Test]
    [TestOf(typeof(DamageableSystem))]
    [TestCaseSource(nameof(_damageables))]
    [Description("Ensures all Entity Prototpes with damageable can be damaged.")]
    public async Task TestDamageableComponents(string damageable)
    {
        var map = await Pair.CreateTestMap();
        var coordinates = new EntityCoordinates(map.CGridUid, Vector2.Zero);

        var entity = await SpawnAtPosition(damageable, coordinates);
        var damageSys = Server.System<DamageableSystem>();

        foreach (var type in Server.ProtoMan.EnumeratePrototypes<DamageTypePrototype>())
        {
            if (!damageSys.CanBeDamagedBy(entity, type))
                continue;

            var damage = new DamageSpecifier(type, FixedPoint2.Epsilon);
            damageSys.ChangeDamage(entity, damage, ignoreResistances: true);
            Assert.That(damageSys.GetTotalDamage(entity) == FixedPoint2.Epsilon);
            damageSys.ClearAllDamage(entity);
        }
    }
}
