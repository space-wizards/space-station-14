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
    public class DamageableTest : ContentIntegrationTest
    {
        private const string DamageableEntityId = "TestDamageableEntityId";
        private const string Group1Id = "TestGroup1";
        private const string Group2Id = "TestGroup2";
        private const string Group3Id = "TestGroup3";
        private string Prototypes = $@"
# Define some damage groups
- type: damageType
  id: TestDamage11

- type: damageType
  id: TestDamage21

- type: damageType
  id: TestDamage22

- type: damageType
  id: TestDamage31

- type: damageType
  id: TestDamage32

- type: damageType
  id: TestDamage33

# Define damage Groups with 1,2,3 damage types
- type: damageGroup
  id: {Group1Id}
  damageTypes:
    - TestDamage11

- type: damageGroup
  id: {Group2Id}
  damageTypes:
    - TestDamage21
    - TestDamage22

- type: damageGroup
  id: {Group3Id}
  damageTypes:
    - TestDamage31
    - TestDamage32
    - TestDamage33

# we want to test a container that supports only full groups
# we will also give full support for group 2 IMPLICITLY by specifying all of its members are supported.
- type: damageContainer
  id: testSomeDamageContainer
  supportedGroups:
    - {Group3Id}
  supportedTypes:
    - TestDamage21
    - TestDamage22

- type: entity
  id: {DamageableEntityId}
  name: {DamageableEntityId}
  components:
  - type: Damageable
    damageContainer: testSomeDamageContainer
";

        /// <summary>
        ///     Test a standard damageable components
        /// </summary>
        /// <remarks>
        ///     Only test scenarios where each damage type is a member of exactly one group, and all damageable components support whole groups, not lone damage types.
        /// </remarks>
        [Test]
        public async Task TestDamageableComponents()
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

            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                // Check that the correct groups are supported by the container
                Assert.That(sDamageableComponent.IsApplicableDamageGroup(group1), Is.False);
                Assert.That(sDamageableComponent.IsApplicableDamageGroup(group2), Is.True);
                Assert.That(sDamageableComponent.IsApplicableDamageGroup(group3), Is.True);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group1), Is.False);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group2), Is.True);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group3), Is.True);

                // Check that the correct types are supported:
                foreach (var group in sPrototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    foreach(var type in group.DamageTypes)
                    {
                        if (sDamageableComponent.IsFullySupportedDamageGroup(group))
                        {
                            Assert.That(sDamageableComponent.IsSupportedDamageType(type), Is.True);
                        }
                        else
                        {
                            Assert.That(sDamageableComponent.IsSupportedDamageType(type), Is.False);
                        }
                    }
                }


                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group1), Is.False);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group2), Is.True);
                Assert.That(sDamageableComponent.IsFullySupportedDamageGroup(group3), Is.True);

                // Check that damage works properly if perfectly divisible among group members
                int damageToDeal, groupDamage, typeDamage; ;
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));
                foreach (var damageGroup in sDamageableComponent.FullySupportedDamageGroups)
                {
                    var types = damageGroup.DamageTypes;

                    // Damage
                    damageToDeal = types.Count() * 5;
                    Assert.That(sDamageableComponent.TryChangeDamage(damageGroup, damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));
                    Assert.That(sDamageableComponent.TryGetDamage(damageGroup, out groupDamage), Is.True);
                    Assert.That(groupDamage, Is.EqualTo(damageToDeal));

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryGetDamage(type, out typeDamage), Is.True);
                        Assert.That(typeDamage, Is.EqualTo(damageToDeal / types.Count()));
                    }

                    // Heal
                    Assert.That(sDamageableComponent.TryChangeDamage(damageGroup, -damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.Zero);
                    Assert.That(sDamageableComponent.TryGetDamage(damageGroup, out groupDamage), Is.True);
                    Assert.That(groupDamage, Is.Zero);

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryGetDamage(type, out typeDamage), Is.True);
                        Assert.That(typeDamage, Is.Zero);
                    }
                }

                // Check that damage works properly if it is NOT perfectly divisible among group members
                foreach (var damageGroup in sDamageableComponent.FullySupportedDamageGroups)
                {
                    var types = damageGroup.DamageTypes;

                    // Damage
                    damageToDeal = types.Count() * 5 - 1;
                    Assert.That(sDamageableComponent.TryChangeDamage(damageGroup, damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));
                    Assert.That(sDamageableComponent.TryGetDamage(damageGroup, out groupDamage), Is.True);
                    Assert.That(groupDamage, Is.EqualTo(damageToDeal));

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryGetDamage(type, out typeDamage), Is.True);
                        float targetDamage = ((float) damageToDeal) / types.Count();
                        Assert.That(typeDamage, Is.InRange(targetDamage - 1, targetDamage + 1));
                    }

                    // Heal
                    Assert.That(sDamageableComponent.TryChangeDamage(damageGroup, -damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.Zero);
                    Assert.That(sDamageableComponent.TryGetDamage(damageGroup, out groupDamage), Is.True);
                    Assert.That(groupDamage, Is.Zero);

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryGetDamage(type, out typeDamage), Is.True);
                        Assert.That(typeDamage, Is.Zero);
                    }
                }

                // Test that unsupported groups return false when setting/getting damage (and don't change damage)
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));
                foreach (var damageGroup in sPrototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    if (sDamageableComponent.IsFullySupportedDamageGroup(damageGroup))
                    {
                        continue;
                    }

                    Assert.That(sDamageableComponent.IsApplicableDamageGroup(damageGroup), Is.False);

                    var types = damageGroup.DamageTypes;
                    damageToDeal = types.Count() * 5;

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.IsSupportedDamageType(type), Is.False);
                    }
;
                    Assert.That(sDamageableComponent.TryChangeDamage(damageGroup, damageToDeal, true), Is.False);
                    Assert.That(sDamageableComponent.TryGetDamage(damageGroup, out groupDamage), Is.False);

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryChangeDamage(type, damageToDeal, true), Is.False);
                        Assert.That(sDamageableComponent.TryGetDamage(type, out typeDamage), Is.False);
                    }
                }
                // Did damage change?
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));


                // Test total damage function
                damageToDeal = 10;

                Assert.True(sDamageableComponent.TryChangeDamage(group3, damageToDeal, true));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));

                var totalTypeDamage = 0;

                foreach (var damageType in sDamageableComponent.SupportedDamageTypes)
                {
                    Assert.True(sDamageableComponent.TryGetDamage(damageType, out typeDamage));
                    Assert.That(typeDamage, Is.LessThanOrEqualTo(damageToDeal));

                    totalTypeDamage += typeDamage;
                }
                Assert.That(totalTypeDamage, Is.EqualTo(damageToDeal));


                // Test healing all damage
                Assert.That(sDamageableComponent.TrySetAllDamage(0));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                // Test preferential healing
                damageToDeal = 12;
                var damageTypes = group3.DamageTypes.ToArray();

                // Deal damage
                Assert.True(sDamageableComponent.TryChangeDamage(damageTypes[0], 17));
                Assert.True(sDamageableComponent.TryChangeDamage(damageTypes[1], 31));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(48));

                // Heal group damage
                Assert.True(sDamageableComponent.TryChangeDamage(group3, -11));

                // Check healing (3 + 9)
                Assert.That(sDamageableComponent.GetDamage(damageTypes[0]), Is.EqualTo(14));
                Assert.That(sDamageableComponent.GetDamage(damageTypes[1]), Is.EqualTo(23));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(37));

                // Heal group damage
                Assert.True(sDamageableComponent.TryChangeDamage(group3, -36));

                // Check healing (13 + 23)
                Assert.That(sDamageableComponent.GetDamage(damageTypes[0]), Is.EqualTo(1));
                Assert.That(sDamageableComponent.GetDamage(damageTypes[1]), Is.EqualTo(0));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(1));

                //Check Damage
                Assert.True(sDamageableComponent.TryGetDamage(damageTypes[0], out typeDamage));
                Assert.That(typeDamage, Is.LessThanOrEqualTo(damageToDeal));
            });
        }


        private const string SharedDamageTypeId = "TestSharedDamage";
        private const string UnsupportedDamageTypeId = "TestUnsupportedDamage";
        private string Prototypes2 = $@"
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
                ExtraPrototypes = Prototypes2
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
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damage + 6));

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
