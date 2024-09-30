using System.IO;
using System.Linq;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    /// <summary>
    ///     Tests that a map's yaml does not change when saved consecutively.
    /// </summary>
    [TestFixture]
    public sealed class SaveLoadSaveTest
    {
        [Test]
        public async Task SaveLoadSave()
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
                mapSystem.CreateMap(out var mapId0);
                // TODO: Properly find the "main" station grid.
                var grid0 = mapManager.CreateGridEntity(mapId0);
                mapLoader.Save(grid0.Owner, "save load save 1.yml");
                mapSystem.CreateMap(out var mapId1);
                EntityUid grid1 = default!;
#pragma warning disable NUnit2045
                Assert.That(mapLoader.TryLoad(mapId1, "save load save 1.yml", out var roots, new MapLoadOptions() { LoadMap = false }), $"Failed to load test map {TestMap}");
                Assert.DoesNotThrow(() =>
                {
                    grid1 = roots.First(uid => entManager.HasComponent<MapGridComponent>(uid));
                });
#pragma warning restore NUnit2045
                mapLoader.Save(grid1, "save load save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            var rp1 = new ResPath("/save load save 1.yml");
            await using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            var rp2 = new ResPath("/save load save 2.yml");
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
            var mapManager = server.ResolveDependency<IMapManager>();
            var mapSystem = server.System<SharedMapSystem>();

            MapId mapId = default;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            // Load bagel.yml as uninitialized map, and save it to ensure it's up to date.
            server.Post(() =>
            {
                mapSystem.CreateMap(out mapId, runMapInit: false);
                mapManager.SetMapPaused(mapId, true);
                Assert.That(mapLoader.TryLoad(mapId, TestMap, out _), $"Failed to load test map {TestMap}");
                mapLoader.SaveMap(mapId, "load save ticks save 1.yml");
            });

            // Run 5 ticks.
            server.RunTicks(5);

            await server.WaitPost(() =>
            {
                mapLoader.SaveMap(mapId, "/load save ticks save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            await using (var stream = userData.Open(new ResPath("/load save ticks save 1.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            await using (var stream = userData.Open(new ResPath("/load save ticks save 2.yml"), FileMode.Open))
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

            await server.WaitPost(() => mapManager.DeleteMap(mapId));
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
            var mapSystem = server.System<SharedMapSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var userData = server.ResolveDependency<IResourceManager>().UserData;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            MapId mapId = default;
            const string fileA = "/load tick load a.yml";
            const string fileB = "/load tick load b.yml";
            string yamlA;
            string yamlB;

            // Load & save the first map
            server.Post(() =>
            {
                mapSystem.CreateMap(out mapId, runMapInit: false);
                mapManager.SetMapPaused(mapId, true);
                Assert.That(mapLoader.TryLoad(mapId, TestMap, out _), $"Failed to load test map {TestMap}");
                mapLoader.SaveMap(mapId, fileA);
            });

            await server.WaitIdleAsync();
            await using (var stream = userData.Open(new ResPath(fileA), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                yamlA = await reader.ReadToEndAsync();
            }

            server.RunTicks(5);

            // Load & save the second map
            server.Post(() =>
            {
                mapManager.DeleteMap(mapId);
                mapSystem.CreateMap(out mapId, runMapInit: false);
                mapManager.SetMapPaused(mapId, true);
                Assert.That(mapLoader.TryLoad(mapId, TestMap, out _), $"Failed to load test map {TestMap}");
                mapLoader.SaveMap(mapId, fileB);
            });

            await server.WaitIdleAsync();

            await using (var stream = userData.Open(new ResPath(fileB), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                yamlB = await reader.ReadToEndAsync();
            }

            Assert.That(yamlA, Is.EqualTo(yamlB));

            await server.WaitPost(() => mapManager.DeleteMap(mapId));
            await pair.CleanReturnAsync();
        }
    }
}
