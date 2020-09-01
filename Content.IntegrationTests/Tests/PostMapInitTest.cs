using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class PostMapInitTest : ContentIntegrationTest
    {
        public readonly string[] SkippedMaps =
        {
            "/Maps/Pathfinding/simple.yml"
        };

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResourcePath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                if (SkippedMaps.Contains(rootedPath.ToString()))
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

                Assert.False(postMapInit);
            }
        }
    }
}
