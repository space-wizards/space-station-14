using System.Linq;
using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    // Tests various scenarios of deleting the entity that a player's mind is connected to.
    [TestFixture]
    public class MindEntityDeletionTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestDeleteVisiting()
        {
            var (_, server) = await StartConnectedServerDummyTickerClientPair();

            EntityUid playerEnt = default;
            EntityUid visitEnt = default;
            Mind mind = null;
            server.Assert(() =>
            {
                var player = IoCManager.Resolve<IPlayerManager>().ServerSessions.Single();

                var mapMan = IoCManager.Resolve<IMapManager>();
                var entMgr = IoCManager.Resolve<IServerEntityManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                playerEnt = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                visitEnt = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);

                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);
                mind.Visit(visitEnt);

                Assert.That(player.AttachedEntity, Is.EqualTo(visitEnt));
                Assert.That(mind.VisitingEntity, Is.EqualTo(visitEnt));
            });

            server.RunTicks(1);

            server.Assert(() =>
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(visitEnt);

                Assert.That(mind.VisitingEntity, Is.EqualTo(default));

                // This used to throw so make sure it doesn't.
                IoCManager.Resolve<IEntityManager>().DeleteEntity(playerEnt);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TestGhostOnDelete()
        {
            // Has to be a non-dummy ticker so we have a proper map.
            var (_, server) = await StartConnectedServerClientPair();

            EntityUid playerEnt = default;
            Mind mind = null;
            server.Assert(() =>
            {
                var player = IoCManager.Resolve<IPlayerManager>().ServerSessions.Single();

                var mapMan = IoCManager.Resolve<IMapManager>();
                var entMgr = IoCManager.Resolve<IServerEntityManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                playerEnt = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);

                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);

                Assert.That(mind.CurrentEntity, Is.EqualTo(playerEnt));
            });

            server.RunTicks(1);

            server.Post(() =>
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity((EntityUid) playerEnt);
            });

            server.RunTicks(1);

            server.Assert(() =>
            {
                Assert.That(IoCManager.Resolve<IEntityManager>().EntityExists(mind.CurrentEntity), Is.True);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TestGhostOnDeleteMap()
        {
            // Has to be a non-dummy ticker so we have a proper map.
            var (_, server) = await StartConnectedServerClientPair();

            EntityUid playerEnt = default;
            Mind mind = null;
            MapId map = default;
            server.Assert(() =>
            {
                var player = IoCManager.Resolve<IPlayerManager>().ServerSessions.Single();

                var mapMan = IoCManager.Resolve<IMapManager>();

                map = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(map);

                var entMgr = IoCManager.Resolve<IServerEntityManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                playerEnt = entMgr.SpawnEntity(null, grid.ToCoordinates());

                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);

                Assert.That(mind.CurrentEntity, Is.EqualTo(playerEnt));
            });

            server.RunTicks(1);

            server.Post(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.DeleteMap(map);
            });

            server.RunTicks(1);

            server.Assert(() =>
            {
                Assert.That(IoCManager.Resolve<IEntityManager>().EntityExists(mind.CurrentEntity), Is.True);
                Assert.That(mind.CurrentEntity, Is.Not.EqualTo(playerEnt));
            });

            await server.WaitIdleAsync();
        }
    }
}
