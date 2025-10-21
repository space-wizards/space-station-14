using System.Linq;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    public sealed class DestructibleDestructionTest
    {
        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();

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
                var coordinates = sEntityManager.GetComponent<TransformComponent>(sDestructibleEntity).Coordinates;
                var bruteDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>(TestBruteDamageGroupId);
                DamageSpecifier bruteDamage = new(bruteDamageGroup, 50);

#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.DoesNotThrow(() =>
                {
                    sEntityManager.System<DamageableSystem>().TryChangeDamage(sDestructibleEntity, bruteDamage, true);
                });

                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));
#pragma warning restore NUnit2045

                var threshold = sTestThresholdListenerSystem.ThresholdsReached[0].Threshold;

                Assert.Multiple(() =>
                {
                    Assert.That(threshold.Triggered, Is.True);
                    Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));
                });

                var spawnEntitiesBehavior = (SpawnEntitiesBehavior)threshold.Behaviors.Single(b => b is SpawnEntitiesBehavior);

                Assert.Multiple(() =>
                {
                    Assert.That(spawnEntitiesBehavior.Spawn, Has.Count.EqualTo(1));
                    Assert.That(spawnEntitiesBehavior.Spawn.Keys.Single(), Is.EqualTo(SpawnedEntityId));
                    Assert.That(spawnEntitiesBehavior.Spawn.Values.Single(), Is.EqualTo(new MinMax { Min = 1, Max = 1 }));
                });

                var entitiesInRange = sEntityManager.System<EntityLookupSystem>().GetEntitiesInRange(coordinates, 3, LookupFlags.All | LookupFlags.Approximate);
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

                Assert.That(found, Is.True, $"Unable to find {SpawnedEntityId} nearby for destructible test; found {entitiesInRange.Count} entities.");
            });
            await pair.CleanReturnAsync();
        }
    }
}
