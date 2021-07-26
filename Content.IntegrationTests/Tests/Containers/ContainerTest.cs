using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Containers
{
    [TestFixture]
    public class ContainerTest : ContentIntegrationTest
    {
        const string PROTOTYPES = @"
- type: entity
  id: dummy
  name: dummy
  components:
  - type: Eye

- type: entity
  id: item
  name: item
";

        /// <summary>
        /// Tests container states with children that do not exist on the client and that when those children are created that they get properly added to the container.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestContainerNonexistantItems()
        {
            var optsServer = new ServerIntegrationOptions { ExtraPrototypes = PROTOTYPES };
            var optsClient = new ClientIntegrationOptions { ExtraPrototypes = PROTOTYPES };
            var (client, server) = await StartConnectedServerDummyTickerClientPair(optsClient, optsServer);

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var mapId = MapId.Nullspace;
            var mapPos = MapCoordinates.Nullspace;

            EntityUid entityUid = default!;
            EntityUid itemUid = default!;

            await server.WaitAssertion(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entMan = IoCManager.Resolve<IEntityManager>();
                var playerMan = IoCManager.Resolve<IPlayerManager>();

                mapId = mapMan.CreateMap();
                mapPos = new MapCoordinates((0, 0), mapId);

                var entity = entMan.SpawnEntity("dummy", mapPos);
                entityUid = entity.Uid;
                playerMan.GetAllPlayers().First().AttachToEntity(entity);

                var container = entity.EnsureContainer<Container>("dummy");

                var item = entMan.SpawnEntity("item", mapPos);
                itemUid = item.Uid;
                container.Insert(item);

                // Move item out of PVS so it doesn't get sent to the client
                item.Transform.LocalPosition = (100000, 0);
            });

            // Needs minimum 4 to sync to client because buffer size is 3
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));

                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                var compMan = IoCManager.Resolve<IComponentManager>();

                // Move item into PVS so it gets sent to the client
                compMan.GetComponent<ITransformComponent>(itemUid).LocalPosition = (0, 0);
            });

            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));

                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(!containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(0));
            });
        }

        /// <summary>
        /// Tests container states with children that do not exist on the client and that when those children are created that they get properly added to the container.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestContainerExpectedEntityDeleted()
        {
            var optsServer = new ServerIntegrationOptions { ExtraPrototypes = PROTOTYPES };
            var optsClient = new ClientIntegrationOptions { ExtraPrototypes = PROTOTYPES };
            var (client, server) = await StartConnectedServerDummyTickerClientPair(optsClient, optsServer);

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var mapId = MapId.Nullspace;
            var mapPos = MapCoordinates.Nullspace;

            EntityUid entityUid = default!;
            EntityUid itemUid = default!;

            await server.WaitAssertion(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entMan = IoCManager.Resolve<IEntityManager>();
                var playerMan = IoCManager.Resolve<IPlayerManager>();

                mapId = mapMan.CreateMap();
                mapPos = new MapCoordinates((0, 0), mapId);

                var entity = entMan.SpawnEntity("dummy", mapPos);
                entityUid = entity.Uid;
                playerMan.GetAllPlayers().First().AttachToEntity(entity);

                var container = entity.EnsureContainer<Container>("dummy");

                var item = entMan.SpawnEntity("item", mapPos);
                itemUid = item.Uid;
                container.Insert(item);

                // Move item out of PVS so it doesn't get sent to the client
                item.Transform.LocalPosition = (100000, 0);
            });

            // Needs minimum 4 to sync to client because buffer size is 3
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));

                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                // If possible it'd be best to only have the DeleteEntity, but right now
                // the entity deleted event is not played on the client if the entity does not exist on the client.
                if (entMan.TryGetEntity(itemUid, out var item)
                    && ContainerHelpers.TryGetContainer(item, out var container))
                    container.ForceRemove(item);
                entMan.DeleteEntity(itemUid);
            });

            await server.WaitRunTicks(1);
            await client.WaitRunTicks(4);

            await client.WaitAssertion(() =>
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                if (!entMan.TryGetEntity(entityUid, out var entity)
                    || !entity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComp))
                {
                    Assert.Fail();
                    return;
                }

                var container = containerManagerComp.GetContainer("dummy");
                Assert.That(container.ContainedEntities.Count, Is.EqualTo(0));

                var containerSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContainerSystem>();
                Assert.That(!containerSystem.ExpectedEntities.ContainsKey(itemUid));
                Assert.That(containerSystem.ExpectedEntities.Count, Is.EqualTo(0));
            });
        }
    }
}
