using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    /// <summary>
    ///     Tests that the
    /// </summary>
    [TestFixture]
    public sealed class SaveLoadSaveTest
    {
        [Test]
        public async Task SaveLoadSave()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings {Fresh = true, Disconnected = true});
            var server = pairTracker.Pair.Server;
            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitPost(() =>
            {
                var mapId0 = mapManager.CreateMap();
                // TODO: Properly find the "main" station grid.
                var grid0 = mapManager.CreateGrid(mapId0);
                mapLoader.Save(grid0.GridEntityId, "save load save 1.yml");
                var mapId1 = mapManager.CreateMap();
                var grid1 = mapLoader.LoadGrid(mapId1, "save load save 1.yml", new MapLoadOptions() {LoadMap = false});
                mapLoader.Save(grid1!.Value, "save load save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            var rp1 = new ResourcePath("/save load save 1.yml");
            await using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            var rp2 = new ResourcePath("/save load save 2.yml");
            await using (var stream = userData.Open(rp2, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = await reader.ReadToEndAsync();
            }

            Assert.Multiple(() => {
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
            await pairTracker.CleanReturnAsync();
        }

        /// <summary>
        ///     Loads the default map, runs it for 5 ticks, then assert that it did not change.
        /// </summary>
        [Test]
        public async Task LoadSaveTicksSaveSaltern()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();

            MapId mapId = default;

            // Load saltern.yml as uninitialized map, and save it to ensure it's up to date.
            server.Post(() =>
            {
                mapId = mapManager.CreateMap();
                mapManager.AddUninitializedMap(mapId);
                mapManager.SetMapPaused(mapId, true);
                mapLoader.LoadMap(mapId, "Maps/saltern.yml");
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

            await using (var stream = userData.Open(new ResourcePath("/load save ticks save 1.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = await reader.ReadToEndAsync();
            }

            await using (var stream = userData.Open(new ResourcePath("/load save ticks save 2.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = await reader.ReadToEndAsync();
            }

            Assert.Multiple(() => {
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
            await pairTracker.CleanReturnAsync();
        }
    }
}
