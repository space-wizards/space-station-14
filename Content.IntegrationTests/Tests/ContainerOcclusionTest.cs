using System.Linq;
using System.Threading.Tasks;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    public sealed class ContainerOcclusionTest
    {
        private const string Prototypes = @"
- type: entity
  id: ContainerOcclusionA
  components:
  - type: EntityStorage
    occludesLight: true

- type: entity
  id: ContainerOcclusionB
  components:
  - type: EntityStorage
    showContents: true
    occludesLight: false

- type: entity
  id: ContainerOcclusionDummy
  components:
  - type: Sprite
  - type: PointLight
";

        [Test]
        public async Task TestA()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ExtraPrototypes = Prototypes});
            var s = pairTracker.Pair.Server;
            var c = pairTracker.Pair.Client;

            var cEntities = c.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var ent2 = s.ResolveDependency<IMapManager>();
            await s.WaitPost(() =>
            {
                var mapId = ent2.GetAllMapIds().Last();
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var ent = IoCManager.Resolve<IEntityManager>();
                var entStorage = ent.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = ent.SpawnEntity("ContainerOcclusionA", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await c.WaitAssertion(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.True(sprite.ContainerOccluded);
                Assert.True(light.ContainerOccluded);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestB()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ExtraPrototypes = Prototypes});
            var s = pairTracker.Pair.Server;
            var c = pairTracker.Pair.Client;

            var cEntities = c.ResolveDependency<IEntityManager>();
            var ent2 = s.ResolveDependency<IMapManager>();

            EntityUid dummy = default;
            await s.WaitPost(() =>
            {
                var mapId = ent2.GetAllMapIds().Last();
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var ent = IoCManager.Resolve<IEntityManager>();
                var entStorage = ent.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = ent.SpawnEntity("ContainerOcclusionB", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await c.WaitAssertion(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.False(sprite.ContainerOccluded);
                Assert.False(light.ContainerOccluded);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestAb()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ExtraPrototypes = Prototypes});
            var s = pairTracker.Pair.Server;
            var c = pairTracker.Pair.Client;

            var ent2 = s.ResolveDependency<IMapManager>();
            var cEntities = c.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            await s.WaitPost(() =>
            {
                var mapId = ent2.GetAllMapIds().Last();
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var ent = IoCManager.Resolve<IEntityManager>();
                var entStorage = ent.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var containerA = ent.SpawnEntity("ContainerOcclusionA", pos);
                var containerB = ent.SpawnEntity("ContainerOcclusionB", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(containerB, containerA);
                entStorage.Insert(dummy, containerB);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await c.WaitAssertion(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.True(sprite.ContainerOccluded);
                Assert.True(light.ContainerOccluded);
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
