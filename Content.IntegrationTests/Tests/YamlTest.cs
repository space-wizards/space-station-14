using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class YamlTest : ContentIntegrationTest
    {
        [Test]
        public async Task ValidYamlDocumentsTest()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var resourceManager = server.ResolveDependency<IResourceManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var filePath in resourceManager.ContentFindFiles(new ResourcePath(@"/Prototypes/")))
                {
                    if (filePath.Extension != "yml" || filePath.Filename.StartsWith(".")) continue;
                    using var reader = new StreamReader(resourceManager.ContentFileRead(filePath), EncodingHelpers.UTF8);
                    var yamlStream = new YamlStream();
                    Assert.DoesNotThrow(() =>
                    {
                        yamlStream.Load(reader);
                    }, $"Error loading yaml {filePath}");
                }
            });
        }
    }
}
