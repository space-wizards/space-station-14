using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.EntitySystems;
using NUnit.Framework;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.DoAfter
{
    [TestFixture]
    [TestOf(typeof(DoAfterComponent))]
    public class DoAfterServerTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            Task<DoAfterStatus> task = null;
            var server = StartServerDummyTicker();
            float tickTime = 0.0f;

            // That it finishes successfully
            server.Post(() =>
            {
                tickTime = 1.0f / IoCManager.Resolve<IGameTiming>().TickRate;
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var mob = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime / 2, cancelToken.Token);
                task = EntitySystem.Get<DoAfterSystem>().DoAfter(args);
            });
            
            await server.WaitRunTicks(1);
            Assert.That(task.Result == DoAfterStatus.Finished);
            
            // That cancel works on mob move
            server.Post(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var mob = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                var cancelToken = new CancellationTokenSource();
                var args = new DoAfterEventArgs(mob, tickTime * 2, cancelToken.Token);
                task = EntitySystem.Get<DoAfterSystem>().DoAfter(args);
                mob.Transform.GridPosition = mob.Transform.GridPosition.Translated(new Vector2(0.1f, 0.1f));
            });

            await server.WaitRunTicks(1);
            Assert.That(task.Result == DoAfterStatus.Cancelled);
        }
    }
}