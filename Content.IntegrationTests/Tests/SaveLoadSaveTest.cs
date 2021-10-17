using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    /// <summary>
    ///     Tests that the
    /// </summary>
    [TestFixture]
    public class SaveLoadSaveTest : ContentIntegrationTest
    {
        [Test]
        public async Task SaveLoadSave()
        {
            var server = StartServer(new ServerContentIntegrationOption {Pool = false});
            await server.WaitIdleAsync();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            server.Post(() =>
            {
                // TODO: Un-hardcode the grid Id for this test.
                mapLoader.SaveBlueprint(new GridId(1), "save load save 1.yml");
                var mapId = mapManager.CreateMap();
                var grid = mapLoader.LoadBlueprint(mapId, "save load save 1.yml");
                mapLoader.SaveBlueprint(grid!.Index, "save load save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            var rp1 = new ResourcePath("/save load save 1.yml");
            using (var stream = userData.Open(rp1, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = reader.ReadToEnd();
            }

            var rp2 = new ResourcePath("/save load save 2.yml");
            using (var stream = userData.Open(rp2, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = reader.ReadToEnd();
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
        }

        /// <summary>
        ///     Loads the default map, runs it for 5 ticks, then assert that it did not change.
        /// </summary>
        [Test]
        public async Task LoadSaveTicksSaveSaltern()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var pauseMgr = server.ResolveDependency<IPauseManager>();

            IMapGrid grid = default;

            // Load saltern.yml as uninitialized map, and save it to ensure it's up to date.
            server.Post(() =>
            {
                var mapId = mapManager.CreateMap();
                pauseMgr.AddUninitializedMap(mapId);
                pauseMgr.SetMapPaused(mapId, true);
                grid = mapLoader.LoadBlueprint(mapId, "Maps/saltern.yml");
                mapLoader.SaveBlueprint(grid.Index, "load save ticks save 1.yml");
            });

            // Run 5 ticks.
            server.RunTicks(5);

            server.Post(() =>
            {
                mapLoader.SaveBlueprint(grid.Index, "/load save ticks save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            using (var stream = userData.Open(new ResourcePath("/load save ticks save 1.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = reader.ReadToEnd();
            }

            using (var stream = userData.Open(new ResourcePath("/load save ticks save 2.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = reader.ReadToEnd();
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
        }
    }
}
