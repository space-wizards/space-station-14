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

            foreach (var ent in entMan.GetEntities().ToArray())
            {
                // Let's skip entities that have been deleted, as we want to get their TransformComp for extra info.
                if (entMan.Deleted(ent))
                {
                    logger.Info($"Skipping {entMan.ToPrettyString(ent)}...");
                    continue;
                }

                // Log some information about the entity before we delete it.
                var transform = entMan.GetComponent<TransformComponent>(ent);
                logger.Info($"Deleting entity {entMan.ToPrettyString(ent)}... Parent: {entMan.ToPrettyString(transform.ParentUid)} | Children: {string.Join(", ", transform.Children.Select(c => entMan.ToPrettyString(c.Owner)))}");

                // Actually delete the entity now.
                entMan.DeleteEntity(ent);
            }
        });
        await pairTracker.CleanReturnAsync();
    }
}
