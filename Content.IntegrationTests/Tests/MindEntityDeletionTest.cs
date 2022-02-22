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
    public sealed class MindEntityDeletionTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestDeleteVisiting()
        {
            var (_, server) = await StartConnectedServerDummyTickerClientPair();

            var entMan = server.ResolveDependency<IServerEntityManager>();
            EntityUid playerEnt = default;
            EntityUid visitEnt = default;
            Mind mind = null;
            server.Assert(() =>
            {
                var player = IoCManager.Resolve<IPlayerManager>().ServerSessions.Single();

                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                playerEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
                visitEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

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
                entMan.DeleteEntity(visitEnt);

                Assert.That(mind.VisitingEntity, Is.EqualTo(default));

                // This used to throw so make sure it doesn't.
                entMan.DeleteEntity(playerEnt);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task TestGhostOnDelete()
        {
            // Has to be a non-dummy ticker so we have a proper map.
            var (_, server) = await StartConnectedServerClientPair();

            var entMan = server.ResolveDependency<IServerEntityManager>();
            EntityUid playerEnt = default;
            Mind mind = null;
            server.Assert(() =>
            {
                var player = IoCManager.Resolve<IPlayerManager>().ServerSessions.Single();

                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                playerEnt = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);

                Assert.That(mind.CurrentEntity, Is.EqualTo(playerEnt));
            });

            server.RunTicks(1);

            server.Post(() =>
            {
                entMan.DeleteEntity(playerEnt);
            });

            server.RunTicks(1);

            server.Assert(() =>
            {
                Assert.That(entMan.EntityExists(mind.CurrentEntity!.Value), Is.True);
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
                Assert.That(IoCManager.Resolve<IEntityManager>().EntityExists(mind.CurrentEntity!.Value), Is.True);
                Assert.That(mind.CurrentEntity, Is.Not.EqualTo(playerEnt));
            });

            await server.WaitIdleAsync();
        }
    }
}
