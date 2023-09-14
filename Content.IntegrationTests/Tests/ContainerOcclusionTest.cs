using System.Numerics;
using Content.Server.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    public sealed class ContainerOcclusionTest
    {
        [TestPrototypes]
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
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var s = pair.Server;
            var c = pair.Client;

            var cEntities = c.ResolveDependency<IEntityManager>();
            var ent = s.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var mapManager = s.ResolveDependency<IMapManager>();
            var mapId = mapManager.CreateMap();

            await s.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var entStorage = ent.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = ent.SpawnEntity("ContainerOcclusionA", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await pair.RunTicksSync(5);

            await c.WaitAssertion(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.Multiple(() =>
                {
                    Assert.That(sprite.ContainerOccluded);
                    Assert.That(light.ContainerOccluded);
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestB()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var s = pair.Server;
            var c = pair.Client;

            var cEntities = c.ResolveDependency<IEntityManager>();
            var ent = s.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var mapManager = s.ResolveDependency<IMapManager>();
            var mapId = mapManager.CreateMap();

            await s.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var entStorage = ent.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = ent.SpawnEntity("ContainerOcclusionB", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await pair.RunTicksSync(5);

            await c.WaitAssertion(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.Multiple(() =>
                {
                    Assert.That(sprite.ContainerOccluded, Is.False);
                    Assert.That(light.ContainerOccluded, Is.False);
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestAb()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
            var s = pair.Server;
            var c = pair.Client;

            var cEntities = c.ResolveDependency<IEntityManager>();
            var ent = s.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var mapManager = s.ResolveDependency<IMapManager>();
            var mapId = mapManager.CreateMap();

            await s.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var entStorage = ent.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var containerA = ent.SpawnEntity("ContainerOcclusionA", pos);
                var containerB = ent.SpawnEntity("ContainerOcclusionB", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(containerB, containerA);
                entStorage.Insert(dummy, containerB);
            });

            await pair.RunTicksSync(5);

            await c.WaitAssertion(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.Multiple(() =>
                {
                    Assert.That(sprite.ContainerOccluded);
                    Assert.That(light.ContainerOccluded);
                });
            });

            await pair.CleanReturnAsync();
        }
    }
}
