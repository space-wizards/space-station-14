using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests
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
            var server = StartServer();
            await server.WaitIdleAsync();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            server.Post(() =>
            {
                mapLoader.SaveBlueprint(new GridId(2), "save load save 1.yml");
                var mapId = mapManager.CreateMap();
                var grid = mapLoader.LoadBlueprint(mapId, "save load save 1.yml");
                mapLoader.SaveBlueprint(grid.Index, "save load save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            using (var stream = userData.Open(new ResourcePath("save load save 1.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = reader.ReadToEnd();
            }

            using (var stream = userData.Open(new ResourcePath("save load save 2.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = reader.ReadToEnd();
            }

            Assert.That(one, Is.EqualTo(two));
        }

        /// <summary>
        ///     Loads the default map, runs it for 5 ticks, then assert that it did not change.
        /// </summary>
        [Test]
        public async Task LoadSaveTicksSaveStationStation()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var pauseMgr = server.ResolveDependency<IPauseManager>();

            IMapGrid grid = default;

            // Load stationstation.yml as uninitialized map, and save it to ensure it's up to date.
            server.Post(() =>
            {
                var mapId = mapManager.CreateMap();
                pauseMgr.AddUninitializedMap(mapId);
                grid = mapLoader.LoadBlueprint(mapId, "Maps/stationstation.yml");
                mapLoader.SaveBlueprint(grid.Index, "load save ticks save 1.yml");
            });

            // Run 5 ticks.
            server.RunTicks(5);

            server.Post(() =>
            {
                mapLoader.SaveBlueprint(grid.Index, "load save ticks save 2.yml");
            });

            await server.WaitIdleAsync();
            var userData = server.ResolveDependency<IResourceManager>().UserData;

            string one;
            string two;

            using (var stream = userData.Open(new ResourcePath("load save ticks save 1.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                one = reader.ReadToEnd();
            }

            using (var stream = userData.Open(new ResourcePath("load save ticks save 2.yml"), FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                two = reader.ReadToEnd();
            }

            Assert.That(one, Is.EqualTo(two));
        }
    }
}
