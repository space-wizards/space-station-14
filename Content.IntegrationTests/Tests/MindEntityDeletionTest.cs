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
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    // Tests various scenarios of deleting the entity that a player's mind is connected to.
    [TestFixture]
    public sealed class MindEntityDeletionTest
    {
        [Test]
        public async Task TestDeleteVisiting()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var entMan = server.ResolveDependency<IServerEntityManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            EntityUid playerEnt = default;
            EntityUid visitEnt = default;
            Mind mind = null;
            var map = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var player = playerMan.ServerSessions.Single();
                var pos = new MapCoordinates(Vector2.Zero, map.MapId);

                playerEnt = entMan.SpawnEntity(null, pos);
                visitEnt = entMan.SpawnEntity(null, pos);

                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);
                mind.Visit(visitEnt);

                Assert.That(player.AttachedEntity, Is.EqualTo(visitEnt));
                Assert.That(mind.VisitingEntity, Is.EqualTo(visitEnt));
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitAssertion(() =>
            {
                entMan.DeleteEntity(visitEnt);
                if (mind == null)
                {
                    Assert.Fail("Mind was null");
                    return;
                }

                if (mind.VisitingEntity != null)
                {
                    Assert.Fail("Mind VisitingEntity was not null");
                    return;
                }

                // This used to throw so make sure it doesn't.
                entMan.DeleteEntity(playerEnt);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(map.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestGhostOnDelete()
        {
            // Has to be a non-dummy ticker so we have a proper map.

            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var entMan = server.ResolveDependency<IServerEntityManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var map = await PoolManager.CreateTestMap(pairTracker);

            EntityUid playerEnt = default;
            Mind mind = null;
            await server.WaitAssertion(() =>
            {
                var player = playerMan.ServerSessions.Single();

                var pos = new MapCoordinates(Vector2.Zero, map.MapId);

                playerEnt = entMan.SpawnEntity(null, pos);

                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);

                Assert.That(mind.CurrentEntity, Is.EqualTo(playerEnt));
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitPost(() =>
            {
                entMan.DeleteEntity(playerEnt);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitAssertion(() =>
            {
                Assert.That(entMan.EntityExists(mind.CurrentEntity!.Value), Is.True);
            });

            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(map.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestGhostOnDeleteMap()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;

            var entMan = server.ResolveDependency<IServerEntityManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var map = await PoolManager.CreateTestMap(pairTracker);

            EntityUid playerEnt = default;
            Mind mind = null;
            await server.WaitAssertion(() =>
            {
                var player = playerMan.ServerSessions.Single();

                playerEnt = entMan.SpawnEntity(null, coordinates);
                mind = new Mind(player.UserId);
                mind.ChangeOwningPlayer(player.UserId);

                mind.TransferTo(playerEnt);

                Assert.That(mind.CurrentEntity, Is.EqualTo(playerEnt));
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitAssertion(() =>
            {
                Assert.That(entMan.EntityExists(mind.CurrentEntity!.Value), Is.True);
                Assert.That(mind.CurrentEntity, Is.Not.EqualTo(playerEnt));
            });

            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(map.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
