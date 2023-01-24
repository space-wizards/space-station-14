using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;
using ShuttleSystem = Content.Server.Shuttles.Systems.ShuttleSystem;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class PostMapInitTest
    {
        private const bool SkipTestMaps = true;
        private const string TestMapsPath = "/Maps/Test/";

        private static readonly string[] NoSpawnMaps =
        {
            "CentComm",
            "Dart",
        };

        private static string[] Grids =
        {
            "/Maps/centcomm.yml",
            "/Maps/Shuttles/cargo.yml",
            "/Maps/Shuttles/emergency.yml",
            "/Maps/infiltrator.yml",
        };

        /// <summary>
        /// Asserts that specific files have been saved as grids and not maps.
        /// </summary>
        [Test, TestCaseSource(nameof(Grids))]
        public async Task GridsLoadableTest(string mapFile)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();
                try
                {
                    mapLoader.LoadGrid(mapId, mapFile);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapFile}, was it saved as a map instead of a grid?", ex);
                }

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapFile}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResourcePath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!resourceManager.TryContentFileRead(rootedPath, out var fileStream))
                {
                    Assert.Fail($"Map not found: {rootedPath}");
                }

                using var reader = new StreamReader(fileStream);
                var yamlStream = new YamlStream();

                yamlStream.Load(reader);

                var root = yamlStream.Documents[0].RootNode;
                var meta = root["meta"];
                var postMapInit = meta["postmapinit"].AsBool();

                Assert.False(postMapInit, $"Map {map.Filename} was saved postmapinit");
            }
            await pairTracker.CleanReturnAsync();
        }

        private static string[] GetGameMapNames()
        {
           Task<string[]> task;
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(static async () =>
                {
                    await Task.Yield();
                    await using var pairTracker = await PoolManager.GetServerClient(
                        new PoolSettings
                        {
                            Disconnected = true,
                            TestName = $"{nameof(PostMapInitTest)}.{nameof(GetGameMapNames)}"
                        }
                    );
                    var server = pairTracker.Pair.Server;
                    var protoManager = server.ResolveDependency<IPrototypeManager>();

                    var maps = protoManager.EnumeratePrototypes<GameMapPrototype>().ToList();
                    var mapNames = new List<string>();
                    var naughty = new HashSet<string>()
                    {
                        "Empty",
                        "Infiltrator",
                        "Pirate",
                    };

                    foreach (var map in maps)
                    {
                        // AAAAAAAAAA
                        // Why are they stations!
                        if (naughty.Contains(map.ID))
                            continue;

                        mapNames.Add(map.ID);
                    }

                    await pairTracker.CleanReturnAsync();
                    return mapNames.ToArray();
                });
                Task.WaitAll(task);
            }

            return task.GetAwaiter().GetResult();
        }

        [Test, TestCaseSource(nameof(GetGameMapNames))]
        public async Task GameMapsLoadableTest(string mapProto)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var ticker = entManager.EntitySysManager.GetEntitySystem<GameTicker>();
            var shuttleSystem = entManager.EntitySysManager.GetEntitySystem<ShuttleSystem>();
            var xformQuery = entManager.GetEntityQuery<TransformComponent>();

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();
                try
                {
                    ticker.LoadGameMap(protoManager.Index<GameMapPrototype>(mapProto), mapId, null);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapProto}", ex);
                }

                var shuttleMap = mapManager.CreateMap();
                var largest = 0f;
                EntityUid? targetGrid = null;
                var memberQuery = entManager.GetEntityQuery<StationMemberComponent>();

                var grids = mapManager.GetAllMapGrids(mapId).ToList();
                var gridUids = grids.Select(o => o.Owner).ToList();

                foreach (var grid in grids)
                {
                    if (!memberQuery.HasComponent(grid.Owner))
                        continue;

                    var area = grid.LocalAABB.Width * grid.LocalAABB.Height;

                    if (area > largest)
                    {
                        largest = area;
                        targetGrid = grid.Owner;
                    }
                }

                // Test shuttle can dock.
                // This is done inside gamemap test because loading the map takes ages and we already have it.
                var station = entManager.GetComponent<StationMemberComponent>(targetGrid!.Value).Station;
                var stationConfig = entManager.GetComponent<StationDataComponent>(station).StationConfig;
                Assert.IsNotNull(stationConfig, $"{entManager.ToPrettyString(station)} had null StationConfig.");
                var shuttlePath = stationConfig.EmergencyShuttlePath.ToString();
                var shuttle = mapLoader.LoadGrid(shuttleMap, shuttlePath);
                Assert.That(shuttle != null && shuttleSystem.TryFTLDock(entManager.GetComponent<ShuttleComponent>(shuttle.Value), targetGrid.Value), $"Unable to dock {shuttlePath} to {mapProto}");

                mapManager.DeleteMap(shuttleMap);

                // Test that the map has valid latejoin spawn points
                if (!NoSpawnMaps.Contains(mapProto))
                {
                    var lateSpawns = 0;

                    foreach (var comp in entManager.EntityQuery<SpawnPointComponent>(true))
                    {
                        if (comp.SpawnType != SpawnPointType.LateJoin ||
                            !xformQuery.TryGetComponent(comp.Owner, out var xform) ||
                            xform.GridUid == null ||
                            !gridUids.Contains(xform.GridUid.Value))
                        {
                            continue;
                        }

                        lateSpawns++;
                        break;
                    }

                    Assert.That(lateSpawns, Is.GreaterThan(0), $"Found no latejoin spawn points on {mapProto}");
                }
                // Test all availableJobs have spawnPoints
                // This is done inside gamemap test because loading the map takes ages and we already have it.
                var jobList = entManager.GetComponent<StationJobsComponent>(station).RoundStartJobList
                    .Where(x => x.Value != 0)
                    .Select(x => x.Key);
                var spawnPoints = entManager.EntityQuery<SpawnPointComponent>()
                    .Where(spawnpoint => spawnpoint.SpawnType == SpawnPointType.Job)
                    .Select(spawnpoint => spawnpoint.Job.ID)
                    .Distinct();
                List<string> missingSpawnPoints = new();
                foreach (var spawnpoint in jobList.Except(spawnPoints))
                {
                    if (protoManager.Index<JobPrototype>(spawnpoint).SetPreference)
                        missingSpawnPoints.Add(spawnpoint);
                }
                Assert.That(missingSpawnPoints.Count() == 0, $"There is no spawnpoint for {String.Join(", ", missingSpawnPoints)} on {mapProto}.");

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapProto}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pairTracker.CleanReturnAsync();
        }

        /// <summary>
        /// Get the non-game map maps.
        /// </summary>
        private static string[] GetMaps()
        {
            Task<string[]> task;
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(static async () =>
                {
                    await Task.Yield();
                    await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{Disconnected = true});
                    var server = pairTracker.Pair.Server;
                    var resourceManager = server.ResolveDependency<IResourceManager>();
                    var protoManager = server.ResolveDependency<IPrototypeManager>();

                    var gameMaps = protoManager.EnumeratePrototypes<GameMapPrototype>().Select(o => o.MapPath).ToHashSet();

                    var mapFolder = new ResourcePath("/Maps");
                    var maps = resourceManager
                        .ContentFindFiles(mapFolder)
                        .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                        .ToArray();
                    var mapNames = new List<string>();
                    foreach (var map in maps)
                    {
                        var rootedPath = map.ToRootedPath();

                        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                        if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath, StringComparison.Ordinal) ||
                            gameMaps.Contains(map))
                        {
                            continue;
                        }
                        mapNames.Add(rootedPath.ToString());
                    }

                    await pairTracker.CleanReturnAsync();
                    return mapNames.ToArray();
                });
                Task.WaitAll(task);
            }

            return task.GetAwaiter().GetResult();
        }

        [Test, TestCaseSource(nameof(GetMaps))]
        public async Task MapsLoadableTest(string mapName)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();
                try
                {
                    mapLoader.LoadMap(mapId, mapName);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapName}", ex);
                }

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapName}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pairTracker.CleanReturnAsync();
        }
    }
}
