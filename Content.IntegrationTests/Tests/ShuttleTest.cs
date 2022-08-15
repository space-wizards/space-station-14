#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ShuttleTest
    {
        private static string[] GetMapPrototypes()
        {
            Task<string[]> task;
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(static async () =>
                {
                    await Task.Yield();
                    await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{Disconnected = true});
                    var server = pairTracker.Pair.Server;
                    var protoManager = server.ResolveDependency<IPrototypeManager>();
                    var maps = new List<string>();

                    foreach (var proto in protoManager.EnumeratePrototypes<GameMapPrototype>())
                    {
                        // Listen I'm a coder not a mapper and waystation doesn't work.
                        if (proto.MapPath.ToString().StartsWith("/Maps/Test/") ||
                            proto.ID == "waystation")
                            continue;

                        maps.Add(proto.ID);
                    }

                    await pairTracker.CleanReturnAsync();
                    return maps.ToArray();
                });

                Task.WaitAll(task);
            }

            return task.GetAwaiter().GetResult();
        }

        [Test, TestCaseSource(nameof(GetMapPrototypes))]
        public async Task EmergencyShuttleDocksTest(string gameMapId)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var loader = server.ResolveDependency<IMapLoader>();
            var ticker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
            var shuttleSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var shuttleMap = mapManager.CreateMap();
                var (_, grids) = ticker.LoadGameMap(protoManager.Index<GameMapPrototype>(gameMapId), mapId, null);
                var largest = 0f;
                EntityUid? targetGrid = null;

                foreach (var grid in grids)
                {
                    var gridGrid = entManager.GetComponent<IMapGridComponent>(grid).Grid;
                    var area = gridGrid.LocalAABB.Width * gridGrid.LocalAABB.Height;

                    if (area > largest)
                    {
                        largest = area;
                        targetGrid = grid;
                    }
                }

                Assert.That(targetGrid, Is.Not.Null);

                var station = entManager.GetComponent<StationMemberComponent>(targetGrid!.Value).Station;
                var shuttle = loader.LoadBlueprint(shuttleMap, entManager.GetComponent<StationDataComponent>(station).EmergencyShuttlePath.ToString());
                Assert.That(shuttleSystem.TryFTLDock(entManager.GetComponent<ShuttleComponent>(shuttle.gridId!.Value), targetGrid.Value));

                mapManager.DeleteMap(mapId);
                mapManager.DeleteMap(shuttleMap);

            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapMan = server.ResolveDependency<IMapManager>();
            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid gridEnt = default;

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(mapId);
                gridEnt = grid.GridEntityId;

                Assert.That(sEntities.TryGetComponent(gridEnt, out ShuttleComponent? shuttleComponent));
                Assert.That(sEntities.TryGetComponent(gridEnt, out PhysicsComponent? physicsComponent));
                Assert.That(physicsComponent!.BodyType, Is.EqualTo(BodyType.Dynamic));
                Assert.That(sEntities.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.EqualTo(Vector2.Zero));
                physicsComponent.ApplyLinearImpulse(Vector2.One);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That<Vector2?>(sEntities.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
