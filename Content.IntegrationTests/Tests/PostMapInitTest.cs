using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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

        private static readonly string[] Grids =
        {
            "/Maps/centcomm.yml",
            "/Maps/Shuttles/cargo.yml",
            "/Maps/Shuttles/emergency.yml",
            "/Maps/infiltrator.yml",
        };

        private static readonly string[] GameMaps =
        {
            "Dev",
            "TestTeg",
            "Fland",
            "Meta",
            "Packed",
            "Aspid",
            "Cluster",
            "Omega",
            "Bagel",
            "Origin",
            "CentComm",
            "Box",
            "Europa",
            "Barratry",
            "Saltern",
            "Core",
            "Marathon",
            "Kettle",
            "MeteorArena"
        };

        /// <summary>
        /// Asserts that specific files have been saved as grids and not maps.
        /// </summary>
        [Test, TestCaseSource(nameof(Grids))]
        public async Task GridsLoadableTest(string mapFile)
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();
                try
                {
#pragma warning disable NUnit2045
                    Assert.That(mapLoader.TryLoad(mapId, mapFile, out var roots));
                    Assert.That(roots.Where(uid => entManager.HasComponent<MapGridComponent>(uid)), Is.Not.Empty);
#pragma warning restore NUnit2045
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

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResPath("/Maps");
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

                Assert.That(postMapInit, Is.False, $"Map {map.Filename} was saved postmapinit");
            }
            await pair.CleanReturnAsync();
        }

        [Test, TestCaseSource(nameof(GameMaps))]
        public async Task GameMapsLoadableTest(string mapProto)
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var ticker = entManager.EntitySysManager.GetEntitySystem<GameTicker>();
            var shuttleSystem = entManager.EntitySysManager.GetEntitySystem<ShuttleSystem>();
            var xformQuery = entManager.GetEntityQuery<TransformComponent>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

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
                targetGrid = gridUids.First();

                foreach (var grid in grids)
                {
                    var gridEnt = grid.Owner;
                    if (!memberQuery.HasComponent(gridEnt))
                        continue;

                    var area = grid.LocalAABB.Width * grid.LocalAABB.Height;

                    if (area > largest)
                    {
                        largest = area;
                        targetGrid = gridEnt;
                    }
                }

                // Test shuttle can dock.
                // This is done inside gamemap test because loading the map takes ages and we already have it.
                var station = entManager.GetComponent<StationMemberComponent>(targetGrid!.Value).Station;
                if (entManager.TryGetComponent<StationEmergencyShuttleComponent>(station, out var stationEvac))
                {
                    var shuttlePath = stationEvac.EmergencyShuttlePath;
#pragma warning disable NUnit2045
                    Assert.That(mapLoader.TryLoad(shuttleMap, shuttlePath.ToString(), out var roots));
                    EntityUid shuttle = default!;
                    Assert.DoesNotThrow(() =>
                    {
                        shuttle = roots.First(uid => entManager.HasComponent<MapGridComponent>(uid));
                    }, $"Failed to load {shuttlePath}");
                    Assert.That(
                        shuttleSystem.TryFTLDock(shuttle,
                            entManager.GetComponent<ShuttleComponent>(shuttle), targetGrid.Value),
                        $"Unable to dock {shuttlePath} to {mapProto}");
#pragma warning restore NUnit2045
                }

                mapManager.DeleteMap(shuttleMap);

                if (entManager.HasComponent<StationJobsComponent>(station))
                {
                    // Test that the map has valid latejoin spawn points
                    if (!NoSpawnMaps.Contains(mapProto))
                    {
                        var lateSpawns = 0;

                        var query = entManager.AllEntityQueryEnumerator<SpawnPointComponent>();
                        while (query.MoveNext(out var uid, out var comp))
                        {
                            if (comp.SpawnType != SpawnPointType.LateJoin
                            || !xformQuery.TryGetComponent(uid, out var xform)
                            || xform.GridUid == null
                            || !gridUids.Contains(xform.GridUid.Value))
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

                    Assert.That(missingSpawnPoints, Has.Count.EqualTo(0),
                        $"There is no spawnpoint for {string.Join(", ", missingSpawnPoints)} on {mapProto}.");
                }

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

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task AllMapsTested()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var gameMaps = protoMan.EnumeratePrototypes<GameMapPrototype>()
                .Where(x => !pair.IsTestPrototype(x))
                .Select(x => x.ID)
                .ToHashSet();

            Assert.That(gameMaps.Remove(PoolManager.TestMap));

            CollectionAssert.AreEquivalent(GameMaps.ToHashSet(), gameMaps, "Game map prototype missing from test cases.");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task NonGameMapsLoadableTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var resourceManager = server.ResolveDependency<IResourceManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            var gameMaps = protoManager.EnumeratePrototypes<GameMapPrototype>().Select(o => o.MapPath).ToHashSet();

            var mapFolder = new ResPath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                .ToArray();

            var mapNames = new List<string>();
            foreach (var map in maps)
            {
                if (gameMaps.Contains(map))
                    continue;

                var rootedPath = map.ToRootedPath();
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath, StringComparison.Ordinal))
                {
                    continue;
                }
                mapNames.Add(rootedPath.ToString());
            }

            await server.WaitPost(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var mapName in mapNames)
                    {
                        var mapId = mapManager.CreateMap();
                        try
                        {
                            Assert.That(mapLoader.TryLoad(mapId, mapName, out _));
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
                    }
                });
            });

            await server.WaitRunTicks(1);
            await pair.CleanReturnAsync();
        }
    }
}
