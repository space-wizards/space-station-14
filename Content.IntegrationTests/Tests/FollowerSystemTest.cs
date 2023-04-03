using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Follower;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests;

[TestFixture, TestOf(typeof(FollowerSystem))]
public sealed class FollowerSystemTest
{
    /// <summary>
    ///     This test ensures that deleting a map while an entity follows another doesn't throw any exceptions.
    /// </summary>
    [Test]
    public async Task FollowerMapDeleteTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new (){NoClient = true});
        var server = pairTracker.Pair.Server;

        await server.WaitPost(() =>
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            var entMan = IoCManager.Resolve<IEntityManager>();
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var logger = IoCManager.Resolve<ILogManager>().RootSawmill;
            var followerSystem = sysMan.GetEntitySystem<FollowerSystem>();

            // Create a map to spawn the observers on.
            var map = mapMan.CreateMap();

            // Spawn an observer to be followed.
            var followed = entMan.SpawnEntity("MobObserver", new MapCoordinates(0, 0, map));
            logger.Info($"Spawned followed observer: {entMan.ToPrettyString(followed)}");

            // Spawn an observer to follow another observer.
            var follower = entMan.SpawnEntity("MobObserver", new MapCoordinates(0, 0, map));
            logger.Info($"Spawned follower observer: {entMan.ToPrettyString(follower)}");

            followerSystem.StartFollowingEntity(follower, followed);

            entMan.DeleteEntity(mapMan.GetMapEntityId(map));
        });
        await pairTracker.CleanReturnAsync();
    }
}
