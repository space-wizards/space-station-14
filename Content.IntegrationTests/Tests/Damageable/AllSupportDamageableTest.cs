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
    public class AllSupportDamageableTest : ContentIntegrationTest
    {
        private const string AllDamageDamageableEntityId = "TestAllDamageDamageableEntityId";

        /// <summary>
        ///     Test a damageContainer with all types supported.
        /// </summary>
        /// <remarks>
        ///     As this should also loads in the damage groups & types in the actual damage.yml, this should also act as a basic test to see if damage.yml is set up properly.
        /// </remarks>
        [Test]
        public async Task TestAllSupportDamageableComponent()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();

            IEntity sFullyDamageableEntity;
            IDamageableComponent sFullyDamageableComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = sMapManager.NextMapId();
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                // When prototypes are loaded using the ExtraPrototypes option, they seem to be loaded first?
                // Or at least, no damage prototypes were loaded in by the time that the damageContainer here is loaded.
                // So for now doing explicit loading of prototypes.
                // I have no idea what I am doing, but it works.
                sPrototypeManager.LoadString($@"
# we want to test the all damage container
- type: damageContainer
  id: testAllDamageContainer
  supportAll: true

# create entities
- type: entity
  id: {AllDamageDamageableEntityId}
  name: {AllDamageDamageableEntityId}
  components:
  - type: Damageable
    damageContainer: testAllDamageContainer
");

                sFullyDamageableEntity = sEntityManager.SpawnEntity(AllDamageDamageableEntityId, coordinates);
                sFullyDamageableComponent = sFullyDamageableEntity.GetComponent<IDamageableComponent>();

            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {

                // First check that there actually are any damage types/groups
                // This test depends on a non-empty damage.yml
                Assert.That(sPrototypeManager.EnumeratePrototypes<DamageTypePrototype>().ToList().Count, Is.GreaterThan(0));
                Assert.That(sPrototypeManager.EnumeratePrototypes<DamageGroupPrototype>().ToList().Count, Is.GreaterThan(0));


                // Can we set and get all damage.
                Assert.That(sFullyDamageableComponent.TrySetAllDamage(-10), Is.False);
                Assert.That(sFullyDamageableComponent.TrySetAllDamage(0), Is.True);

                // Test that the all damage container supports every damage type, and that we can get, set, and change
                // every type with the expected results. Notable: if the damage does not change, they all return false
                var initialDamage = 10;
                foreach (var damageType in sPrototypeManager.EnumeratePrototypes<DamageTypePrototype>())
                {
                    var damage = initialDamage;
                    Assert.That(sFullyDamageableComponent.IsSupportedDamageType(damageType));
                    Assert.That(sFullyDamageableComponent.TrySetDamage(damageType, -damage), Is.False);
                    Assert.That(sFullyDamageableComponent.TrySetDamage(damageType, damage), Is.True);
                    Assert.That(sFullyDamageableComponent.TrySetDamage(damageType, damage), Is.True); // intentional duplicate
                    Assert.That(sFullyDamageableComponent.GetDamage(damageType), Is.EqualTo(damage));
                    Assert.That(sFullyDamageableComponent.TryChangeDamage(damageType, -damage / 2, true), Is.True);
                    Assert.That(sFullyDamageableComponent.TryGetDamage(damageType, out damage), Is.True);
                    Assert.That(damage, Is.EqualTo(initialDamage/2));
                    Assert.That(sFullyDamageableComponent.TryChangeDamage(damageType, damage, true), Is.True);
                    Assert.That(sFullyDamageableComponent.GetDamage(damageType), Is.EqualTo(2* damage));
                    Assert.That(sFullyDamageableComponent.TryChangeDamage(damageType, 0, true), Is.False);
                }
                // And again, for every group
                foreach (var damageGroup in sPrototypeManager.EnumeratePrototypes<DamageGroupPrototype>())
                {
                    var damage = initialDamage;
                    var groupSize = damageGroup.DamageTypes.Count();
                    Assert.That(sFullyDamageableComponent.IsFullySupportedDamageGroup(damageGroup));
                    Assert.That(sFullyDamageableComponent.IsApplicableDamageGroup(damageGroup));
                    Assert.That(sFullyDamageableComponent.TrySetDamage(damageGroup, -damage), Is.False);
                    Assert.That(sFullyDamageableComponent.TrySetDamage(damageGroup, damage), Is.True);
                    Assert.That(sFullyDamageableComponent.TrySetDamage(damageGroup, damage), Is.True); // intentional duplicate
                    Assert.That(sFullyDamageableComponent.GetDamage(damageGroup), Is.EqualTo(damage * groupSize));
                    Assert.That(sFullyDamageableComponent.TryChangeDamage(damageGroup, -groupSize*damage / 2, true), Is.True);
                    Assert.That(sFullyDamageableComponent.TryGetDamage(damageGroup, out damage), Is.True);
                    Assert.That(damage, Is.EqualTo(groupSize* initialDamage/2));
                    Assert.That(sFullyDamageableComponent.TryChangeDamage(damageGroup, damage, true), Is.True);
                    Assert.That(sFullyDamageableComponent.GetDamage(damageGroup), Is.EqualTo(2*damage));
                    Assert.That(sFullyDamageableComponent.TryChangeDamage(damageGroup, 0, true), Is.False);
                }
            });
        }
    }
}
