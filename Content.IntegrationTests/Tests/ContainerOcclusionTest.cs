using System.Linq;
using System.Threading.Tasks;
using Content.Server.Storage.Components;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    public class ContainerOcclusionTest : ContentIntegrationTest
    {
        private const string ExtraPrototypes = @"
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

        private async Task<(ClientIntegrationInstance c, ServerIntegrationInstance s)> Start()
        {
            var optsServer = new ServerIntegrationOptions
            {
                CVarOverrides =
                {
                    {CVars.NetPVS.Name, "false"}
                },
                ExtraPrototypes = ExtraPrototypes
            };
            var optsClient = new ClientIntegrationOptions
            {

                CVarOverrides =
                {
                    {CVars.NetPVS.Name, "false"}
                },
                ExtraPrototypes = ExtraPrototypes
            };

            var (c, s) = await StartConnectedServerDummyTickerClientPair(optsClient, optsServer);

            s.Post(() =>
            {
                IoCManager.Resolve<IPlayerManager>().ServerSessions.Single().JoinGame();

                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateMap(new MapId(1));
            });

            return (c, s);
        }

        [Test]
        public async Task TestA()
        {
            var (c, s) = await Start();

            await c.WaitIdleAsync();

            var cEntities = c.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            s.Post(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, new MapId(1));
                var ent = IoCManager.Resolve<IEntityManager>();
                var container = ent.SpawnEntity("ContainerOcclusionA", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                ent.GetComponent<EntityStorageComponent>(container).Insert(dummy);
            });

            await RunTicksSync(c, s, 5);

            c.Assert(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.True(sprite.ContainerOccluded);
                Assert.True(light.ContainerOccluded);
            });

            await Task.WhenAll(c.WaitIdleAsync(), s.WaitIdleAsync());
        }

        [Test]
        public async Task TestB()
        {
            var (c, s) = await Start();

            await c.WaitIdleAsync();

            var cEntities = c.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            s.Post(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, new MapId(1));
                var ent = IoCManager.Resolve<IEntityManager>();
                var container = ent.SpawnEntity("ContainerOcclusionB", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                ent.GetComponent<EntityStorageComponent>(container).Insert(dummy);
            });

            await RunTicksSync(c, s, 5);

            c.Assert(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.False(sprite.ContainerOccluded);
                Assert.False(light.ContainerOccluded);
            });

            await Task.WhenAll(c.WaitIdleAsync(), s.WaitIdleAsync());
        }

        [Test]
        public async Task TestAb()
        {
            var (c, s) = await Start();

            await c.WaitIdleAsync();

            var cEntities = c.ResolveDependency<IEntityManager>();

            EntityUid dummy = default;
            s.Post(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, new MapId(1));
                var ent = IoCManager.Resolve<IEntityManager>();
                var containerA = ent.SpawnEntity("ContainerOcclusionA", pos);
                var containerB = ent.SpawnEntity("ContainerOcclusionB", pos);
                dummy = ent.SpawnEntity("ContainerOcclusionDummy", pos);

                ent.GetComponent<EntityStorageComponent>(containerA).Insert(containerB);
                ent.GetComponent<EntityStorageComponent>(containerB).Insert(dummy);
            });

            await RunTicksSync(c, s, 5);

            c.Assert(() =>
            {
                var sprite = cEntities.GetComponent<SpriteComponent>(dummy);
                var light = cEntities.GetComponent<PointLightComponent>(dummy);
                Assert.True(sprite.ContainerOccluded);
                Assert.True(light.ContainerOccluded);
            });

            await Task.WhenAll(c.WaitIdleAsync(), s.WaitIdleAsync());
        }
    }
}
