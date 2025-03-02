using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Station.Components;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.IoC;
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
            "/Maps/_Harmony/centcomm.yml", // Harmony CC version
            "/Maps/Shuttles/cargo.yml",
            "/Maps/Shuttles/emergency.yml",
            "/Maps/Shuttles/infiltrator.yml",
            AdminTestArenaSystem.ArenaMapPath
        };

        private static readonly string[] DoNotMapWhitelist =
        {
            "/Maps/centcomm.yml",
            "/Maps/_Harmony/centcomm.yml", // Harmony CC version
            "/Maps/_Harmony/barratry.yml", // Harmony map - Contains mime and clown's rubber stamps.
            "/Maps/_Harmony/xeno.yml", // Harmony map - Contains Matter Dematerializer
            "/Maps/_Harmony/Nonstations/listening-outpost.yml", // Harmony - Contains a billion illegal things
            "/Maps/_Harmony/Shuttles/ERT-chaplain-medium.yml", // Harmony - Contains centcomm folders.
            "/Maps/_Harmony/Shuttles/syndicate-diplomacy.yml", // Harmony - Contains syndicate rubber stamp.
            "/Maps/_Harmony/Shuttles/syndicate-dropship.yml", // Harmony - Contains syndicate rubber stamp.
            "/Maps/_Harmony/Shuttles/syndicate-paramedic.yml", // Harmony - Contains syndicate rubber stamp.
            "/Maps/_Harmony/Shuttles/WizDen-ERT-Shuttles/ERT-Small-Deathsquad.yml", // Harmony - Contains handheld crew monitor.
            "/Maps/bagel.yml", // Contains mime's rubber stamp --> Either fix this, remove the category, or remove this comment if intentional.
            "/Maps/gate.yml", // Contains positronic brain and LSE-1200c "Perforator"
            "/Maps/meta.yml", // Contains warden's rubber stamp
            "/Maps/reach.yml", // Contains handheld crew monitor
            "/Maps/Shuttles/ShuttleEvent/cruiser.yml", // Contains LSE-1200c "Perforator"
            "/Maps/Shuttles/ShuttleEvent/honki.yml", // Contains golden honker, clown's rubber stamp
            "/Maps/Shuttles/ShuttleEvent/instigator.yml", // Contains EXP-320g "Friendship"
            "/Maps/Shuttles/ShuttleEvent/syndie_evacpod.yml", // Contains syndicate rubber stamp
        };

        private static readonly string[] GameMaps =
        {
            "dm01-entryway", // Harmony, deathmatch PROMOD map by Unisol
            "dm02-sandbomb", // Harmony, deathmatch PROMOD map by Unisol
            "Xeno", // Harmony, playtest for upstream by SlamBamActionman
            "Barratry", // Harmony, revived by Spanky
            "Aspid", // Harmony, playtest for upstream by Golinth
            "Atlas", // Harmony revived by Kravin
            "Mira", // Harmony, developed by tanuko
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
            "Gate",
            "Amber",
            "Loop",
            "Plasma",
            "Elkridge",
            "Convex",
            "Relic"
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
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);
            var path = new ResPath(mapFile);

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out var mapId);
                try
                {
                    Assert.That(mapLoader.TryLoadGrid(mapId, path, out var grid));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapFile}, was it saved as a map instead of a grid?", ex);
                }

                mapSystem.DeleteMap(mapId);
            });
            await server.WaitRunTicks(1);

            await pair.CleanReturnAsync();
        }

        /// <summary>
        /// Asserts that shuttles are loadable and have been saved as grids and not maps.
        /// </summary>
        [Test]
        public async Task ShuttlesLoadableTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entManager = server.ResolveDependency<IEntityManager>();
            var resMan = server.ResolveDependency<IResourceManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            var shuttleFolder = new ResPath("/Maps/Shuttles");
            var shuttles = resMan
                .ContentFindFiles(shuttleFolder)
                .Where(filePath =>
                    filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                .ToArray();

            await server.WaitPost(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var path in shuttles)
                    {
                        mapSystem.CreateMap(out var mapId);
                        try
                        {
                            Assert.That(mapLoader.TryLoadGrid(mapId, path, out _),
                                $"Failed to load shuttle {path}, was it saved as a map instead of a grid?");
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to load shuttle {path}, was it saved as a map instead of a grid?",
                                ex);
                        }
                        mapSystem.DeleteMap(mapId);
                    }
                });
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
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var loader = server.System<MapLoaderSystem>();

            var mapFolder = new ResPath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith(".", StringComparison.Ordinal))
                .ToArray();

            var v7Maps = new List<ResPath>();
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
                var version = meta["format"].AsInt();

                // TODO MAP TESTS
                // Move this to some separate test?
                CheckDoNotMap(map, root, protoManager);

                if (version >= 7)
                {
                    v7Maps.Add(map);
                    continue;
                }

                var postMapInit = meta["postmapinit"].AsBool();
                Assert.That(postMapInit, Is.False, $"Map {map.Filename} was saved postmapinit");
            }

            var deps = server.ResolveDependency<IEntitySystemManager>().DependencyCollection;
            foreach (var map in v7Maps)
            {
                Assert.That(IsPreInit(map, loader, deps));
            }

            // Check that the test actually does manage to catch post-init maps and isn't just blindly passing everything.
            // To that end, create a new post-init map and try verify it.
            var mapSys = server.System<SharedMapSystem>();
            MapId id = default;
            await server.WaitPost(() => mapSys.CreateMap(out id, runMapInit: false));
            await server.WaitPost(() => server.EntMan.Spawn(null, new MapCoordinates(0, 0, id)));

            // First check that a pre-init version passes
            var path = new ResPath($"{nameof(NoSavedPostMapInitTest)}.yml");
            Assert.That(loader.TrySaveMap(id, path));
            Assert.That(IsPreInit(path, loader, deps));

            // and the post-init version fails.
            await server.WaitPost(() => mapSys.InitializeMap(id));
            Assert.That(loader.TrySaveMap(id, path));
            Assert.That(IsPreInit(path, loader, deps), Is.False);

            await pair.CleanReturnAsync();
        }

        /// <summary>
        /// Check that maps do not have any entities that belong to the DoNotMap entity category
        /// </summary>
        private void CheckDoNotMap(ResPath map, YamlNode node, IPrototypeManager protoManager)
        {
            if (DoNotMapWhitelist.Contains(map.ToString()))
                return;

            var yamlEntities = node["entities"];
            if (!protoManager.TryIndex<EntityCategoryPrototype>("DoNotMap", out var dnmCategory))
                return;

            Assert.Multiple(() =>
            {
                foreach (var yamlEntity in (YamlSequenceNode)yamlEntities)
                {
                    var protoId = yamlEntity["proto"].AsString();

                    // This doesn't properly handle prototype migrations, but thats not a significant issue.
                    if (!protoManager.TryIndex(protoId, out var proto, false))
                        continue;

                    Assert.That(!proto.Categories.Contains(dnmCategory),
                        $"\nMap {map} contains entities in the DO NOT MAP category ({proto.Name})");
                }
            });
        }

        private bool IsPreInit(ResPath map, MapLoaderSystem loader, IDependencyCollection deps)
        {
            if (!loader.TryReadFile(map, out var data))
            {
                Assert.Fail($"Failed to read {map}");
                return false;
            }

            var reader = new EntityDeserializer(deps, data, DeserializationOptions.Default);
            if (!reader.TryProcessData())
            {
                Assert.Fail($"Failed to process {map}");
                return false;
            }

            foreach (var mapId in reader.MapYamlIds)
            {
                var mapData = reader.YamlEntities[mapId];
                if (mapData.PostInit)
                    return false;
            }

            return true;
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
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitPost(() =>
            {
                MapId mapId;
                try
                {
                    var opts = DeserializationOptions.Default with {InitializeMaps = true};
                    ticker.LoadGameMap(protoManager.Index<GameMapPrototype>(mapProto), out mapId, opts);
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
                    Assert.That(mapLoader.TryLoadGrid(shuttleMap, shuttlePath, out var shuttle),
                        $"Failed to load {shuttlePath}");

                    Assert.That(
                        shuttleSystem.TryFTLDock(shuttle!.Value.Owner,
                            entManager.GetComponent<ShuttleComponent>(shuttle!.Value.Owner),
                            targetGrid.Value),
                        $"Unable to dock {shuttlePath} to {mapProto}");
                }

                mapSystem.DeleteMap(shuttleMap);

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
                    mapSystem.DeleteMap(mapId);
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

            var mapPaths = new List<ResPath>();
            foreach (var map in maps)
            {
                if (gameMaps.Contains(map))
                    continue;

                var rootedPath = map.ToRootedPath();
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath, StringComparison.Ordinal))
                {
                    continue;
                }
                mapPaths.Add(rootedPath);
            }

            await server.WaitPost(() =>
            {
                Assert.Multiple(() =>
                {
                    // This bunch of files contains a random mixture of both map and grid files.
                    // TODO MAPPING organize files
                    var opts = MapLoadOptions.Default with
                    {
                        DeserializationOptions = DeserializationOptions.Default with
                        {
                            InitializeMaps = true,
                            LogOrphanedGrids = false
                        }
                    };

                    HashSet<Entity<MapComponent>> maps;
                    foreach (var path in mapPaths)
                    {
                        try
                        {
                            Assert.That(mapLoader.TryLoadGeneric(path, out maps, out _, opts));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to load map {path}", ex);
                        }

                        try
                        {
                            foreach (var map in maps)
                            {
                                server.EntMan.DeleteEntity(map);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to delete map {path}", ex);
                        }
                    }
                });
            });

            await server.WaitRunTicks(1);
            await pair.CleanReturnAsync();
        }
    }
}
