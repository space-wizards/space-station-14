#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Server.Players;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Minds
{
    // Tests various scenarios of deleting the entity that a player's mind is connected to.
    [TestFixture]
    public sealed class MindEntityDeletionTest
    {
        // This test will do the following:
        // - spawn a  player
        // - visit some entity
        // - delete the entity being visited
        // - assert that player returns to original entity
        [Test]
        public async Task TestDeleteVisiting()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var entMan = server.ResolveDependency<IServerEntityManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            EntityUid playerEnt = default;
            EntityUid visitEnt = default;
            Mind mind = default!;
            var map = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var player = playerMan.ServerSessions.Single();
                var pos = new MapCoordinates(Vector2.Zero, map.MapId);

                playerEnt = entMan.SpawnEntity(null, pos);
                visitEnt = entMan.SpawnEntity(null, pos);

                mind = mindSystem.CreateMind(player.UserId);
                mindSystem.TransferTo(mind, playerEnt);
                mindSystem.Visit(mind, visitEnt);

                Assert.That(player.AttachedEntity, Is.EqualTo(visitEnt));
                Assert.That(mind.VisitingEntity, Is.EqualTo(visitEnt));
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);
            await server.WaitPost(() => entMan.DeleteEntity(visitEnt));
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            Assert.IsNull(mind.VisitingEntity);
            Assert.That(entMan.EntityExists(mind.OwnedEntity));
            Assert.That(mind.OwnedEntity, Is.EqualTo(playerEnt));

            // This used to throw so make sure it doesn't.
            await server.WaitPost(() => entMan.DeleteEntity(mind.OwnedEntity!.Value));
            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await server.WaitPost(() => mapManager.DeleteMap(map.MapId));
            await pairTracker.CleanReturnAsync();
        }

        // this is a variant of TestGhostOnDelete that just deletes the whole map.
        [Test]
        public async Task TestGhostOnDeleteMap()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;

            var entMan = server.ResolveDependency<IServerEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var player = playerMan.ServerSessions.Single();

            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var map = await PoolManager.CreateTestMap(pairTracker);

            EntityUid playerEnt = default;
            Mind mind = default!;
            await server.WaitAssertion(() =>
            {
                playerEnt = entMan.SpawnEntity(null, coordinates);
                mind = player.ContentData()!.Mind!;
                mindSystem.TransferTo(mind, playerEnt);

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
