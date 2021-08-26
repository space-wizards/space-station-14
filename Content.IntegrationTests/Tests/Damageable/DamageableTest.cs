using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Damageable
{
    [TestFixture]
    [TestOf(typeof(DamageableComponent))]
    [TestOf(typeof(DamageableSystem))]
    public class DamageableTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
# Define some damage groups
- type: damageType
  id: TestDamage1

- type: damageType
  id: TestDamage2a

- type: damageType
  id: TestDamage2b

- type: damageType
  id: TestDamage3a

- type: damageType
  id: TestDamage3b

- type: damageType
  id: TestDamage3c

# Define damage Groups with 1,2,3 damage types
- type: damageGroup
  id: TestGroup1
  damageTypes:
    - TestDamage1

- type: damageGroup
  id: TestGroup2
  damageTypes:
    - TestDamage2a
    - TestDamage2b

- type: damageGroup
  id: TestGroup3
  damageTypes:
    - TestDamage3a
    - TestDamage3b
    - TestDamage3c

- type: resistanceSet
  id: testResistances
# this space is intentionally left blank

# This container should not support TestDamage1 or TestDamage2b
- type: damageContainer
  id: testDamageContainer
  defaultResistanceSet: testResistances
  supportedGroups:
    - TestGroup3
  supportedTypes:
    - TestDamage2a

- type: entity
  id: TestDamageableEntityId
  name: TestDamageableEntityId
  components:
  - type: Damageable
    damageContainer: testDamageContainer
";

        // public bool & function to determine whether dealing damage resulted in actual damage change
        public bool DamageChanged = false;
        public void DamageChangedListener(EntityUid _, DamageableComponent comp, DamageChangedEvent args)
        {
            DamageChanged = true;
        }

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
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            sEntityManager.EventBus.SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(DamageChangedListener);

            IEntity sDamageableEntity = null;
            DamageableComponent sDamageableComponent = null;
            DamageableSystem sDamageableSystem = null;

            DamageGroupPrototype group1 = default!;
            DamageGroupPrototype group2 = default!;
            DamageGroupPrototype group3 = default!;

            DamageTypePrototype type1 = default!;
            DamageTypePrototype type2a = default!;
            DamageTypePrototype type2b = default!;
            DamageTypePrototype type3a = default!;
            DamageTypePrototype type3b = default!;
            DamageTypePrototype type3c = default!;

            int typeDamage, groupDamage;

            await server.WaitPost(() =>
            {
                var mapId = sMapManager.NextMapId();
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDamageableEntity = sEntityManager.SpawnEntity("TestDamageableEntityId", coordinates);
                sDamageableComponent = sDamageableEntity.GetComponent<DamageableComponent>();
                sDamageableSystem = sEntitySystemManager.GetEntitySystem<DamageableSystem>();

                group1 = sPrototypeManager.Index<DamageGroupPrototype>("TestGroup1");
                group2 = sPrototypeManager.Index<DamageGroupPrototype>("TestGroup2");
                group3 = sPrototypeManager.Index<DamageGroupPrototype>("TestGroup3");

                type1 = sPrototypeManager.Index<DamageTypePrototype>("TestDamage1");
                type2a = sPrototypeManager.Index<DamageTypePrototype>("TestDamage2a");
                type2b = sPrototypeManager.Index<DamageTypePrototype>("TestDamage2b");
                type3a = sPrototypeManager.Index<DamageTypePrototype>("TestDamage3a");
                type3b = sPrototypeManager.Index<DamageTypePrototype>("TestDamage3b");
                type3c = sPrototypeManager.Index<DamageTypePrototype>("TestDamage3c");
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var uid = sDamageableEntity.Uid;

                // Check that the correct types are supported.
                Assert.That(sDamageableComponent.DamagePerType.ContainsKey(type1), Is.False);
                Assert.That(sDamageableComponent.DamagePerType.ContainsKey(type2a), Is.True);
                Assert.That(sDamageableComponent.DamagePerType.ContainsKey(type2b), Is.False);
                Assert.That(sDamageableComponent.DamagePerType.ContainsKey(type3a), Is.True);
                Assert.That(sDamageableComponent.DamagePerType.ContainsKey(type3b), Is.True);
                Assert.That(sDamageableComponent.DamagePerType.ContainsKey(type3c), Is.True);

                // Check that damage is evenly distributed over a group if its a nice multiple
                var types = group3.DamageTypes;
                var damageToDeal = types.Count() * 5;
                DamageData damageData = new(group3, damageToDeal);

                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(damageData, true));
                Assert.That(DamageChanged);
                DamageChanged = false;
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));
                Assert.That(sDamageableComponent.DamagePerGroup[group3], Is.EqualTo(damageToDeal));
                foreach (var type in types)
                {
                    Assert.That(sDamageableComponent.DamagePerType.TryGetValue(type, out typeDamage));
                    Assert.That(typeDamage, Is.EqualTo(damageToDeal / types.Count()));
                }

                // Heal
                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(-damageData));
                Assert.That(DamageChanged);
                DamageChanged = false;
                Assert.That(sDamageableComponent.TotalDamage, Is.Zero);
                Assert.That(sDamageableComponent.DamagePerGroup[group3], Is.EqualTo(0));
                foreach (var type in types)
                {
                    Assert.That(sDamageableComponent.DamagePerType.TryGetValue(type, out typeDamage));
                    Assert.That(typeDamage, Is.Zero);
                }

                // Check that damage works properly if it is NOT perfectly divisible among group members
                types = group3.DamageTypes;
                damageToDeal = types.Count() * 5 - 1;
                damageData = new(group3, damageToDeal);
                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(damageData, true));
                Assert.That(DamageChanged);
                DamageChanged = false;
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));
                Assert.That(sDamageableComponent.DamagePerGroup[group3], Is.EqualTo(damageToDeal));
                // integer rounding. In this case, first member gets 1 less than others.
                Assert.That(sDamageableComponent.DamagePerType[type3a], Is.EqualTo(damageToDeal / types.Count())); 
                Assert.That(sDamageableComponent.DamagePerType[type3b], Is.EqualTo(1 + damageToDeal / types.Count()));
                Assert.That(sDamageableComponent.DamagePerType[type3c], Is.EqualTo(1 + damageToDeal / types.Count())); 

                // Heal
                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(-damageData));
                Assert.That(DamageChanged);
                DamageChanged = false;
                Assert.That(sDamageableComponent.TotalDamage, Is.Zero);
                Assert.That(sDamageableComponent.DamagePerGroup[group3], Is.EqualTo(0));
                foreach (var type in types)
                {
                    Assert.That(sDamageableComponent.DamagePerType.TryGetValue(type, out typeDamage));
                    Assert.That(typeDamage, Is.Zero);
                }

                // Test that unsupported groups return false when setting/getting damage (and don't change damage)
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));
                damageData = new DamageData(group1, 10) + new DamageData(type2b, 10);
                TryChangeDamageEvent damageEvent = new(damageData, true);
                sEntityManager.EventBus.RaiseLocalEvent(uid, damageEvent);
                Assert.That(DamageChanged, Is.False);
                Assert.That(sDamageableComponent.DamagePerGroup.TryGetValue(group1, out groupDamage), Is.False);
                Assert.That(sDamageableComponent.DamagePerType.TryGetValue(type1, out typeDamage), Is.False);
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                // Test SetAll function
                sDamageableSystem.SetAllDamage(sDamageableComponent, 10);
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(10 * sDamageableComponent.DamagePerType.Count()));
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                // Test 'wasted' healing
                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(new DamageData(type3a, 5), true));
                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(new DamageData(type3b, 7), true));
                sEntityManager.EventBus.RaiseLocalEvent(uid, new TryChangeDamageEvent(new DamageData(group3, -11), true));
                Assert.That(sDamageableComponent.DamagePerType[type3a], Is.EqualTo(2));
                Assert.That(sDamageableComponent.DamagePerType[type3b], Is.EqualTo(3));
                Assert.That(sDamageableComponent.DamagePerType[type3c], Is.EqualTo(0));

                // Test Over-Healing
                damageEvent = new TryChangeDamageEvent(new DamageData(group3, -100));
                sEntityManager.EventBus.RaiseLocalEvent(uid, damageEvent);
                Assert.That(DamageChanged);
                DamageChanged = false;
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                // Test that if no health change occurred, returns false
                damageEvent = new TryChangeDamageEvent(new DamageData(group3, -100));
                sEntityManager.EventBus.RaiseLocalEvent(uid, damageEvent);
                Assert.That(DamageChanged, Is.False);
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));
            });
        }
    }
}
