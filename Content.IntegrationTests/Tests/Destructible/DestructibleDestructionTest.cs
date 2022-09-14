using System.Linq;
using System.Threading.Tasks;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    public sealed class DestructibleDestructionTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            EntityUid sDestructibleEntity = default;
            TestDestructibleListenerSystem sTestThresholdListenerSystem = null;

            await server.WaitPost(() =>
            {
                var coordinates = testMap.GridCoords;

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDestructionEntityId, coordinates);
                sTestThresholdListenerSystem = sEntitySystemManager.GetEntitySystem<TestDestructibleListenerSystem>();
                sTestThresholdListenerSystem.ThresholdsReached.Clear();
            });

            await server.WaitAssertion(() =>
            {
                var coordinates = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(sDestructibleEntity).Coordinates;
                var bruteDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>("TestBrute");
                DamageSpecifier bruteDamage = new(bruteDamageGroup,50);

                Assert.DoesNotThrow(() =>
                {
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(sDestructibleEntity, bruteDamage, true);
                });

                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                var threshold = sTestThresholdListenerSystem.ThresholdsReached[0].Threshold;

                Assert.That(threshold.Triggered, Is.True);
                Assert.That(threshold.Behaviors.Count, Is.EqualTo(3));

                var spawnEntitiesBehavior = (SpawnEntitiesBehavior) threshold.Behaviors.Single(b => b is SpawnEntitiesBehavior);

                Assert.That(spawnEntitiesBehavior.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnEntitiesBehavior.Spawn.Keys.Single(), Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnEntitiesBehavior.Spawn.Values.Single(), Is.EqualTo(new MinMax {Min = 1, Max = 1}));

                var entitiesInRange = EntitySystem.Get<EntityLookupSystem>().GetEntitiesInRange(coordinates, 2);
                var found = false;

                foreach (var entity in entitiesInRange)
                {
                    if (sEntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype == null)
                    {
                        continue;
                    }

                    if (sEntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.Name != SpawnedEntityId)
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                Assert.That(found, Is.True);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
