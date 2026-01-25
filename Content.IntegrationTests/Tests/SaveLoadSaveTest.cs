using System.IO;
using System.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    /// <summary>
    ///     Tests that a grid's yaml does not change when saved consecutively.
    /// </summary>
    [TestFixture]
    public sealed class SaveLoadSaveTest
    {
        [Test]
        public async Task CreateSaveLoadSaveGrid()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = entManager.System<MapLoaderSystem>();
            var mapSystem = entManager.System<SharedMapSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            var rp1 = new ResPath("/save load save 1.yml");
            var rp2 = new ResPath("/save load save 2.yml");

            await server.WaitPost(() =>
            {
                mapSystem.CreateMap(out var mapId0);
                var grid0 = mapManager.CreateGridEntity(mapId0);
                entManager.RunMapInit(grid0.Owner, entManager.GetComponent<MetaDataComponent>(grid0));
                Assert.That(mapLoader.TrySaveGrid(grid0.Owner, rp1));
                mapSystem.CreateMap(out var mapId1);
                Assert.That(mapLoader.TryLoadGrid(mapId1, rp1, out var grid1));
                Assert.That(mapLoader.TrySaveGrid(grid1!.Value, rp2));
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            await using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            await using (var stream = userData.Open(rp2, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = await reader.ReadToEndAsync();
            }

            Assert.Multiple(() =>
            {
                Assert.That(two, Is.EqualTo(one));
                var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
                if (failed != null)
                {
                    var oneTmp = Path.GetTempFileName();
                    var twoTmp = Path.GetTempFileName();

                    File.WriteAllText(oneTmp, one);
                    File.WriteAllText(twoTmp, two);

                    TestContext.AddTestAttachment(oneTmp, "First save file");
                    TestContext.AddTestAttachment(twoTmp, "Second save file");
                    TestContext.Error.WriteLine("Complete output:");
                    TestContext.Error.WriteLine(oneTmp);
                    TestContext.Error.WriteLine(twoTmp);
                }
            });
            testSystem.Enabled = false;
            await pair.CleanReturnAsync();
        }

        private const string TestMap = "Maps/bagel.yml";

        /// <summary>
        ///     Loads the default map, runs it for 5 ticks, then assert that it did not change.
        /// </summary>
        [Test]
        public async Task LoadSaveTicksSaveBagel()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapSys = server.System<SharedMapSystem>();
            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            var rp1 = new ResPath("/load save ticks save 1.yml");
            var rp2 = new ResPath("/load save ticks save 2.yml");

            MapId mapId = default;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            // Load bagel.yml as uninitialized map, and save it to ensure it's up to date.
            server.Post(() =>
            {
                var path = new ResPath(TestMap);
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
                mapId = map!.Value.Comp.MapId;
                Assert.That(mapLoader.TrySaveMap(mapId, rp1));
            });

            // Run 5 ticks.
            server.RunTicks(5);

            await server.WaitPost(() =>
            {
                Assert.That(mapLoader.TrySaveMap(mapId, rp2));
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            await using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            await using (var stream = userData.Open(rp2, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = await reader.ReadToEndAsync();
            }

            Assert.Multiple(() =>
            {
                Assert.That(two, Is.EqualTo(one));
                var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
                if (failed != null)
                {
                    var oneTmp = Path.GetTempFileName();
                    var twoTmp = Path.GetTempFileName();

                    File.WriteAllText(oneTmp, one);
                    File.WriteAllText(twoTmp, two);

                    TestContext.AddTestAttachment(oneTmp, "First save file");
                    TestContext.AddTestAttachment(twoTmp, "Second save file");
                    TestContext.Error.WriteLine("Complete output:");
                    TestContext.Error.WriteLine(oneTmp);
                    TestContext.Error.WriteLine(twoTmp);
                }
            });

            testSystem.Enabled = false;
            await server.WaitPost(() => mapSys.DeleteMap(mapId));
            await pair.CleanReturnAsync();
        }

        /// <summary>
        ///     Loads the same uninitialized map at slightly different times, and then checks that they are the same
        ///     when getting saved.
        /// </summary>
        /// <remarks>
        ///     Should ensure that entities do not perform randomization prior to initialization and should prevents
        ///     bugs like the one discussed in github.com/space-wizards/RobustToolbox/issues/3870. This test is somewhat
        ///     similar to <see cref="LoadSaveTicksSaveBagel"/> and <see cref="SaveLoadSave"/>, but neither of these
        ///     caught the mentioned bug.
        /// </remarks>
        [Test]
        public async Task LoadTickLoadBagel()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var mapLoader = server.System<MapLoaderSystem>();
            var mapSys = server.System<SharedMapSystem>();
            var userData = server.ResolveDependency<IResourceManager>().UserData;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);
            var testSystem = server.System<SaveLoadSaveTestSystem>();
            testSystem.Enabled = true;

            MapId mapId1 = default;
            MapId mapId2 = default;
            var fileA = new ResPath("/load tick load a.yml");
            var fileB = new ResPath("/load tick load b.yml");
            string yamlA;
            string yamlB;

            // Load & save the first map
            server.Post(() =>
            {
                var path = new ResPath(TestMap);
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
                mapId1 = map!.Value.Comp.MapId;
                Assert.That(mapLoader.TrySaveMap(mapId1, fileA));
            });

            await server.WaitIdleAsync();
            await using (var stream = userData.Open(fileA, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                yamlA = await reader.ReadToEndAsync();
            }

            server.RunTicks(5);

            // Load & save the second map
            server.Post(() =>
            {
                var path = new ResPath(TestMap);
                Assert.That(mapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
                mapId2 = map!.Value.Comp.MapId;
                Assert.That(mapLoader.TrySaveMap(mapId2, fileB));
            });

            await server.WaitIdleAsync();

            await using (var stream = userData.Open(fileB, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                yamlB = await reader.ReadToEndAsync();
            }

            Assert.That(yamlA, Is.EqualTo(yamlB));

            testSystem.Enabled = false;
            await server.WaitPost(() => mapSys.DeleteMap(mapId1));
            await server.WaitPost(() => mapSys.DeleteMap(mapId2));
            await pair.CleanReturnAsync();
        }

        /// <summary>
        /// Simple system that modifies the data saved to a yaml file by removing the timestamp.
        /// Required by some tests that validate that re-saving a map does not modify it.
        /// </summary>
        private sealed class SaveLoadSaveTestSystem : EntitySystem
        {
            public bool Enabled;
            public override void Initialize()
            {
                SubscribeLocalEvent<AfterSerializationEvent>(OnAfterSave);
            }

            private void OnAfterSave(AfterSerializationEvent ev)
            {
                if (!Enabled)
                    return;

                // Remove timestamp.
                ((MappingDataNode)ev.Node["meta"]).Remove("time");
            }
        }
    }
}
