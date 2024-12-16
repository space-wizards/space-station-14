using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Station.Components;
using FastAccessors;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

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
            "Dart"
        };

        private static readonly string[] Grids =
        {
            "/Maps/centcomm.yml",
            "/Maps/Shuttles/cargo.yml",
            "/Maps/Shuttles/emergency.yml",
            "/Maps/Shuttles/infiltrator.yml",
        };

        private static readonly string[] GameMaps =
        {
            "Dev",
            "TestTeg",
            "Fland",
            "Meta",
            "Packed",
            "Omega",
            "Bagel",
            "CentComm",
            "Box",
            "Core",
            "Marathon",
            "MeteorArena",
            "Saltern",
            "Reach",
            "Train",
            "Oasis",
            "Cog",
            "Amber"
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
            var mapSystem = entManager.System<SharedMapSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out var mapId);
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
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                Dirty = true // Stations spawn a bunch of nullspace entities and maps like centcomm.
            });
            var server = pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var ticker = entManager.EntitySysManager.GetEntitySystem<GameTicker>();
            var shuttleSystem = entManager.EntitySysManager.GetEntitySystem<ShuttleSystem>();
            var xformQuery = entManager.GetEntityQuery<TransformComponent>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out var mapId);
                try
                {
                    ticker.LoadGameMap(protoManager.Index<GameMapPrototype>(mapProto), mapId, null);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapProto}", ex);
                }

                mapSystem.CreateMap(out var shuttleMap);
                var largest = 0f;
                EntityUid? targetGrid = null;
                var memberQuery = entManager.GetEntityQuery<StationMemberComponent>();

                var grids = mapManager.GetAllGrids(mapId).ToList();
                var gridUids = grids.Select(o => o.Owner).ToList();
                targetGrid = gridUids.First();

                foreach (var grid in grids)
                {
                    var gridEnt = grid.Owner;
                    if (!memberQuery.HasComponent(gridEnt))
                        continue;

                    var area = grid.Comp.LocalAABB.Width * grid.Comp.LocalAABB.Height;

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
                    // Test that the map has valid latejoin spawn points or container spawn points
                    if (!NoSpawnMaps.Contains(mapProto))
                    {
                        var lateSpawns = 0;

                        lateSpawns += GetCountLateSpawn<SpawnPointComponent>(gridUids, entManager);
                        lateSpawns += GetCountLateSpawn<ContainerSpawnPointComponent>(gridUids, entManager);

                        Assert.That(lateSpawns, Is.GreaterThan(0), $"Found no latejoin spawn points on {mapProto}");
                    }

                    // Test all availableJobs have spawnPoints
                    // This is done inside gamemap test because loading the map takes ages and we already have it.
                    var comp = entManager.GetComponent<StationJobsComponent>(station);
                    var jobs = new HashSet<ProtoId<JobPrototype>>(comp.SetupAvailableJobs.Keys);

                    var spawnPoints = entManager.EntityQuery<SpawnPointComponent>()
                        .Where(x => x.SpawnType == SpawnPointType.Job && x.Job != null)
                        .Select(x => x.Job.Value);

                    jobs.ExceptWith(spawnPoints);

                    spawnPoints = entManager.EntityQuery<ContainerSpawnPointComponent>()
                        .Where(x => x.SpawnType is SpawnPointType.Job or SpawnPointType.Unset && x.Job != null)
                        .Select(x => x.Job.Value);

                    jobs.ExceptWith(spawnPoints);

                    Assert.That(jobs, Is.Empty, $"There is no spawnpoints for {string.Join(", ", jobs)} on {mapProto}.");
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



        private static int GetCountLateSpawn<T>(List<EntityUid> gridUids, IEntityManager entManager)
            where T : ISpawnPoint, IComponent
        {
            var resultCount = 0;
            var queryPoint = entManager.AllEntityQueryEnumerator<T, TransformComponent>();
#nullable enable
            while (queryPoint.MoveNext(out T? comp, out var xform))
            {
                var spawner = (ISpawnPoint) comp;

                if (spawner.SpawnType is not SpawnPointType.LateJoin
                || xform.GridUid == null
                || !gridUids.Contains(xform.GridUid.Value))
                {
                    continue;
                }
#nullable disable
                resultCount++;
                break;
            }

            return resultCount;
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

            Assert.That(gameMaps, Is.EquivalentTo(GameMaps.ToHashSet()), "Game map prototype missing from test cases.");

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
            var mapSystem = server.System<SharedMapSystem>();
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
                        mapSystem.CreateMap(out var mapId);
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
