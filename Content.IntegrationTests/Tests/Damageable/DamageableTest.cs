using System;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Damageable
{
    [TestFixture]
    [TestOf(typeof(DamageableComponent))]
    public class DamageableTest : ContentIntegrationTest
    {
        private const string DamageableEntityId = "DamageableEntityId";

        private static readonly string Prototypes = $@"
- type: entity
  id: {DamageableEntityId}
  name: {DamageableEntityId}
  components:
  - type: Damageable
    damageContainer: allDamageContainer";

        [Test]
        public async Task TestDamageTypeDamageAndHeal()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDamageableEntity;
            IDamageableComponent sDamageableComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = sMapManager.NextMapId();
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDamageableEntity = sEntityManager.SpawnEntity(DamageableEntityId, coordinates);
                sDamageableComponent = sDamageableEntity.GetComponent<IDamageableComponent>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                var damageToDeal = 7;

                foreach (var type in Enum.GetValues<DamageType>())
                {
                    Assert.That(sDamageableComponent.SupportsDamageType(type));

                    // Damage
                    Assert.That(sDamageableComponent.ChangeDamage(type, damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));
                    Assert.That(sDamageableComponent.TryGetDamage(type, out var damage), Is.True);
                    Assert.That(damage, Is.EqualTo(damageToDeal));

                    // Heal
                    Assert.That(sDamageableComponent.ChangeDamage(type, -damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.Zero);
                    Assert.That(sDamageableComponent.TryGetDamage(type, out damage), Is.True);
                    Assert.That(damage, Is.Zero);
                }
            });
        }

        [Test]
        public async Task TestDamageClassDamageAndHeal()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDamageableEntity;
            IDamageableComponent sDamageableComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = sMapManager.NextMapId();
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDamageableEntity = sEntityManager.SpawnEntity(DamageableEntityId, coordinates);
                sDamageableComponent = sDamageableEntity.GetComponent<IDamageableComponent>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                foreach (var @class in Enum.GetValues<DamageClass>())
                {
                    Assert.That(sDamageableComponent.SupportsDamageClass(@class));

                    var types = @class.ToTypes();

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.SupportsDamageType(type));
                    }

                    var damageToDeal = types.Count * 5;

                    // Damage
                    Assert.That(sDamageableComponent.ChangeDamage(@class, damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(damageToDeal));
                    Assert.That(sDamageableComponent.TryGetDamage(@class, out var classDamage), Is.True);
                    Assert.That(classDamage, Is.EqualTo(damageToDeal));

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryGetDamage(type, out var typeDamage), Is.True);
                        Assert.That(typeDamage, Is.EqualTo(damageToDeal / types.Count));
                    }

                    // Heal
                    Assert.That(sDamageableComponent.ChangeDamage(@class, -damageToDeal, true), Is.True);
                    Assert.That(sDamageableComponent.TotalDamage, Is.Zero);
                    Assert.That(sDamageableComponent.TryGetDamage(@class, out classDamage), Is.True);
                    Assert.That(classDamage, Is.Zero);

                    foreach (var type in types)
                    {
                        Assert.That(sDamageableComponent.TryGetDamage(type, out var typeDamage), Is.True);
                        Assert.That(typeDamage, Is.Zero);
                    }
                }
            });
        }

        [Test]
        public async Task TotalDamageTest()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDamageableEntity;
            IDamageableComponent sDamageableComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = sMapManager.NextMapId();
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDamageableEntity = sEntityManager.SpawnEntity(DamageableEntityId, coordinates);
                sDamageableComponent = sDamageableEntity.GetComponent<IDamageableComponent>();
            });

            await server.WaitAssertion(() =>
            {
                var damageType = DamageClass.Brute;
                var damage = 10;

                Assert.True(sDamageableComponent.ChangeDamage(DamageClass.Brute, damage, true));
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(10));

                var totalTypeDamage = 0;

                foreach (var type in damageType.ToTypes())
                {
                    Assert.True(sDamageableComponent.TryGetDamage(type, out var typeDamage));
                    Assert.That(typeDamage, Is.LessThanOrEqualTo(damage));

                    totalTypeDamage += typeDamage;
                }

                Assert.That(totalTypeDamage, Is.EqualTo(damage));
            });
        }
    }
}
