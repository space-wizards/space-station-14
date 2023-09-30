using Content.Shared.Follower;
using Robust.Shared.GameObjects;
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
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var sysMan = server.ResolveDependency<IEntitySystemManager>();
        var logMan = server.ResolveDependency<ILogManager>();
        var logger = logMan.RootSawmill;

        await server.WaitPost(() =>
        {
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
        await pair.CleanReturnAsync();
    }
}
