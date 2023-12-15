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
            var server = pair.Server;
            var client = pair.Client;

            var clientEntManager = client.ResolveDependency<IEntityManager>();
            var serverEntManager = server.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var mapManager = server.ResolveDependency<IMapManager>();
            var mapId = mapManager.CreateMap();

            await server.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var entStorage = serverEntManager.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = serverEntManager.SpawnEntity("ContainerOcclusionA", pos);
                dummy = serverEntManager.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await pair.RunTicksSync(5);

            var clientEnt = clientEntManager.GetEntity(serverEntManager.GetNetEntity(dummy));

            await client.WaitAssertion(() =>
            {
                var sprite = clientEntManager.GetComponent<SpriteComponent>(clientEnt);
                var light = clientEntManager.GetComponent<PointLightComponent>(clientEnt);
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
            var server = pair.Server;
            var client = pair.Client;

            var clientEntManager = client.ResolveDependency<IEntityManager>();
            var serverEntManager = server.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var mapManager = server.ResolveDependency<IMapManager>();
            var mapId = mapManager.CreateMap();

            await server.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var entStorage = serverEntManager.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = serverEntManager.SpawnEntity("ContainerOcclusionB", pos);
                dummy = serverEntManager.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await pair.RunTicksSync(5);

            var clientEnt = clientEntManager.GetEntity(serverEntManager.GetNetEntity(dummy));

            await client.WaitAssertion(() =>
            {
                var sprite = clientEntManager.GetComponent<SpriteComponent>(clientEnt);
                var light = clientEntManager.GetComponent<PointLightComponent>(clientEnt);
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
            var server = pair.Server;
            var client = pair.Client;

            var clientEntManager = client.ResolveDependency<IEntityManager>();
            var serverEntManager = server.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            var mapManager = server.ResolveDependency<IMapManager>();
            var mapId = mapManager.CreateMap();

            await server.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, mapId);
                var entStorage = serverEntManager.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var containerA = serverEntManager.SpawnEntity("ContainerOcclusionA", pos);
                var containerB = serverEntManager.SpawnEntity("ContainerOcclusionB", pos);
                dummy = serverEntManager.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(containerB, containerA);
                entStorage.Insert(dummy, containerB);
            });

            await pair.RunTicksSync(5);

            var clientEnt = clientEntManager.GetEntity(serverEntManager.GetNetEntity(dummy));

            await client.WaitAssertion(() =>
            {
                var sprite = clientEntManager.GetComponent<SpriteComponent>(clientEnt);
                var light = clientEntManager.GetComponent<PointLightComponent>(clientEnt);
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
