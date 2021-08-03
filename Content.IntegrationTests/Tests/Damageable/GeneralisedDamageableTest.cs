using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Damageable
{
    [TestFixture]
    [TestOf(typeof(DamageableComponent))]
    public class GeneralisedDamageableTest : ContentIntegrationTest
    {
        private const string DamageableEntityId = "TestDamageableEntityId";
        private const string Group1Id = "TestGroup1";
        private const string Group2Id = "TestGroup2";
        private const string Group3Id = "TestGroup3";
        private const string SharedDamageTypeId = "TestSharedDamage";
        private const string UnsupportedDamageTypeId = "TestUnsupportedDamage";
        private string Prototypes = $@"
- type: damageType
  id: {SharedDamageTypeId}

- type: damageType
  id: {UnsupportedDamageTypeId}

- type: damageType
  id: TestDamage1

- type: damageType
  id: TestDamage2

- type: damageGroup
  id: {Group1Id}
  damageTypes:
    - {SharedDamageTypeId}

- type: damageGroup
  id: {Group2Id}
  damageTypes:
    - {SharedDamageTypeId}
    - TestDamage1

- type: damageGroup
  id: {Group3Id}
  damageTypes:
    - {SharedDamageTypeId}
    - TestDamage2
    - {UnsupportedDamageTypeId}

# we want to test a container that only partially supports a group:
- type: damageContainer
  id: TestPartiallySupported
  supportedGroups:
    - {Group2Id}
  supportedTypes:
    - TestDamage2
    - TestDamage1
# does NOT support type {UnsupportedDamageTypeId}, and thus does not fully support group {Group3Id}
# TestDamage1 is added twice because it is also in {Group2Id}. This should not cause errors.

# create entities
- type: entity
  id: {DamageableEntityId}
  name: {DamageableEntityId}
  components:
  - type: Damageable
    damageContainer: TestPartiallySupported
";

        /// <summary>
        ///     Generalized damageable component tests.
        /// </summary>
        /// <remarks>
        ///     Test scenarios where damage types are members of more than one group, or where a component only supports a subset of a group.
        /// </remarks>
        [Test]
        public async Task TestGeneralizedDamageableComponent()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();

            IEntity sDamageableEntity;
            IDamageableComponent sDamageableComponent = null;

            DamageGroupPrototype group1 = default!;
            DamageGroupPrototype group2 = default!;
            DamageGroupPrototype group3 = default!;

            DamageTypePrototype SharedDamageType = default!;
            DamageTypePrototype UnsupportedDamageType = default!;

            await server.WaitPost(() =>
            {
                var mapId = sMapManager.NextMapId();
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDamageableEntity = sEntityManager.SpawnEntity(DamageableEntityId, coordinates);
                sDamageableComponent = sDamageableEntity.GetComponent<IDamageableComponent>();

                group1 = sPrototypeManager.Index<DamageGroupPrototype>(Group1Id);
                group2 = sPrototypeManager.Index<DamageGroupPrototype>(Group2Id);
                group3 = sPrototypeManager.Index<DamageGroupPrototype>(Group3Id);

                SharedDamageType = sPrototypeManager.Index<DamageTypePrototype>(SharedDamageTypeId);
                UnsupportedDamageType = sPrototypeManager.Index<DamageTypePrototype>(UnsupportedDamageTypeId);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                // All damage types should be applicable
                Assert.That(sDamageableComponent.IsApplicableDamageGroup(group1), Is.True);
                Assert.That(sDamageableComponent.IsApplicableDamageGroup(group2), Is.True);
                Assert.That(sDamageableComponent.IsApplicableDamageGroup(group3), Is.True);

                // But not all should be fully supported
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group1), Is.True);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group2), Is.True);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group3), Is.False);

                // Check that the correct damage types are supported
                Assert.That(sDamageableComponent.IsSupportedDamageType(SharedDamageType), Is.True);

                // Check that if we deal damage using a type appearing in multiple groups, nothing goes wrong.
                var damage = 12;
                Assert.That(sDamageableComponent.TryChangeDamage(SharedDamageType, damage), Is.True);
                Assert.That(sDamageableComponent.GetDamage(SharedDamageType), Is.EqualTo(damage));
                Assert.That(sDamageableComponent.GetDamage(group1), Is.EqualTo(damage));
                Assert.That(sDamageableComponent.GetDamage(group2), Is.EqualTo(damage));
                Assert.That(sDamageableComponent.GetDamage(group3), Is.EqualTo(damage));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damage));

                // Check that if we deal damage using a group that is not fully supported, the damage is reduced
                // Note that if damage2 were not neatly divisible by 3, the actual damage reduction would be subject to integer rounding.
                // How much exactly the damage gets reduced then would depend on the order that the groups were defined in the yaml file
                // Here we deal 9 damage. It should apply 3 damage to each type, but one type is ignored, resulting in 6 total damage.
                // However, the damage in group2 and group3 only changes because of one type that overlaps, so they only change by 3
                Assert.That(sDamageableComponent.TryChangeDamage(group3, 9), Is.True);
                Assert.That(sDamageableComponent.GetDamage(group1), Is.EqualTo(damage + 3));
                Assert.That(sDamageableComponent.GetDamage(group2), Is.EqualTo(damage + 3));
                Assert.That(sDamageableComponent.GetDamage(group3), Is.EqualTo(damage + 6));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damage+6));

                // Now we check that when healing, no damage is wasted.
                // Because SharedDamageType has the most damage in group3 (15 vs 3), it will be healed more than the other.
                // Expect that, up to integer rounding, one is healed 5* more than the other.
                // We will use a number that does not divide nicely, there will be some integer rounding.
                Assert.That(sDamageableComponent.TryChangeDamage(group3, -7), Is.True);
                Assert.That(sDamageableComponent.GetDamage(group1), Is.EqualTo(damage + 3 - 5));
                Assert.That(sDamageableComponent.GetDamage(group2), Is.EqualTo(damage + 3 - 5));
                Assert.That(sDamageableComponent.GetDamage(group3), Is.EqualTo(damage + 6 - 7));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damage + 6 - 7));

            });
        }

    }
}
