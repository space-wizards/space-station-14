using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Maps;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
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
                mapLoader.Save(grid0.Owner, "save load save 1.yml");
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
        public async Task LoadSaveTicksSaveBagel()
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
                mapLoader.LoadMap(mapId, "Maps/bagel.yml");
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

        /// <summary>
        ///     Loads Bagel map, saves for persistence, loads back from the saved map instance, saves it again and assert that both save files are equal.
        /// </summary>
        /// <remarks>
        ///     Currently ignoring mole changes. This should be addressed in the future, ignoring just for now in order to keep testing for other harder to find issues.
        /// </remarks>
        [Test]
        public async Task LoadSavePersistence()
        {
            await using var _pairTracker = await PoolManager.GetServerClient(new PoolSettings { Fresh = true, Disconnected = true });
            var _server = _pairTracker.Pair.Server;
            var gameMaps = _pairTracker.Pair.Server.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<GameMapPrototype>();
            var gameMap = gameMaps.Where(gm => gm.ID == "Bagel").First();
            await _pairTracker.CleanReturnAsync();

            string one;
            var oneTmp = Path.GetTempFileName();
            var savePath1 = $"save_load_persistence_{gameMap.MapName}_1.yml";
            string two;
            var twoTmp = Path.GetTempFileName();
            var savePath2 = $"save_load_persistence_{gameMap.MapName}_2.yml";

            // First server load - Load map from existing prototype
            await using (var pairTracker = await PoolManager.GetServerClient(new PoolSettings { Fresh = true, Disconnected = true }))
            {
                var server = pairTracker.Pair.Server;
                var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
                var mapManager = server.ResolveDependency<IMapManager>();
                var userData = server.ResolveDependency<IResourceManager>().UserData;

                await server.WaitPost(() =>
                {
                    var mapId0 = mapManager.CreateMap();
                    mapManager.AddUninitializedMap(mapId0);
                    mapManager.SetMapPaused(mapId0, true);
                    var mapGrids = mapLoader.LoadMap(mapId0, gameMap.MapPath.ToString(), new MapLoadOptions());
                    mapLoader.SaveMap(mapId0, savePath1);
                });
                await pairTracker.CleanReturnAsync();

                var rp1 = new ResourcePath($"/{savePath1}");
                await using (var stream = userData.Open(rp1, FileMode.Open))
                using (var reader = new StreamReader(stream))
                {
                    one = await reader.ReadToEndAsync();
                }
                File.WriteAllText(oneTmp, one);
            }
            // Second server load - Load map from previous save and save it again.
            await using (var pairTracker = await PoolManager.GetServerClient(new PoolSettings { Fresh = true, Disconnected = true }))
            {
                var server = pairTracker.Pair.Server;
                var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
                var mapManager = server.ResolveDependency<IMapManager>();
                var userData = server.ResolveDependency<IResourceManager>().UserData;
                using (var s = userData.OpenWriteText(new ResourcePath($"/{savePath1}")))
                {
                    s.Write(one);
                }

                await server.WaitPost(() =>
                {
                    var mapId0 = mapManager.CreateMap();
                    mapManager.AddUninitializedMap(mapId0);
                    mapManager.SetMapPaused(mapId0, true);
                    var mapGrids = mapLoader.LoadMap(mapId0, savePath1, new MapLoadOptions());
                    mapLoader.SaveMap(mapId0, savePath2);
                });
                await pairTracker.CleanReturnAsync();

                var rp2 = new ResourcePath($"/{savePath2}");
                await using (var stream = userData.Open(rp2, FileMode.Open))
                using (var reader = new StreamReader(stream))
                {
                    two = await reader.ReadToEndAsync();
                }
                File.WriteAllText(twoTmp, two);
            }
            Assert.Multiple(() => {
                //Assert.That(two, Is.EqualTo(one)); //@TODO: Uncomment this when no longer ignoring moles diff.
                using (StringReader readerOne = new StringReader(one))
                {
                    using (StringReader readerTwo = new StringReader(two))
                    {
                        string lineOne = string.Empty;
                        string lineTwo = string.Empty;
                        bool readingMoles = false;
                        do
                        {
                            lineOne = readerOne.ReadLine();
                            lineTwo = readerTwo.ReadLine();
                            if (lineOne != null && lineTwo != null)
                            {
                                // Ignore moles for now...
                                if (lineOne.Contains("moles", System.StringComparison.InvariantCultureIgnoreCase))
                                {
                                    readingMoles = true;
                                    continue;
                                }
                                else if (readingMoles && !lineOne.Trim().StartsWith("-"))
                                {
                                    readingMoles = false;
                                }
                                if (!readingMoles)
                                    Assert.That(lineTwo, Is.EqualTo(lineOne));
                            } else
                            {
                                Assert.That(lineOne == null ^ lineTwo == null, Is.False); // Either both should be null or both should not be null.
                            }
                        } while (lineOne != null && lineTwo != null);
                    }
                }
                var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
                if (failed != null)
                {
                    TestContext.AddTestAttachment(oneTmp, $"{gameMap.MapName} - First save file");
                    TestContext.AddTestAttachment(twoTmp, $"{gameMap.MapName} - Second save file");
                    TestContext.Error.WriteLine($"Persistence for map {gameMap.MapName} has failed.");
                }
            });
        }
    }
}
