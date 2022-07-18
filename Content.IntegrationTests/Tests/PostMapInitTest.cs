using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class PostMapInitTest
    {
        private const bool SkipTestMaps = true;
        private const string TestMapsPath = "/Maps/Test/";

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResourcePath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath))
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

        private static string[] GetMapNames()
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
                    var mapFolder = new ResourcePath("/Maps");
                    var maps = resourceManager
                        .ContentFindFiles(mapFolder)
                        .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                        .ToArray();
                    var mapNames = new List<string>();
                    foreach (var map in maps)
                    {
                        var rootedPath = map.ToRootedPath();

                        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                        if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath))
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

        [Test, TestCaseSource(nameof(GetMapNames))]
        public async Task MapsLoadableTest(string mapName)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var mapLoader = server.ResolveDependency<IMapLoader>();
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
