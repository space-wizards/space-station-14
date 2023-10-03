using Content.Server.Entry;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class ConfigPresetTests
{
    [Test]
    public async Task TestLoadAll()
    {
        var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var resources = server.ResolveDependency<IResourceManager>();
        var config = server.ResolveDependency<IConfigurationManager>();

        await server.WaitPost(() =>
        {
            var presets = resources.ContentFindFiles(EntryPoint.ConfigPresetsDir);

            foreach (var preset in presets)
            {
                var stream = resources.ContentFileRead(preset);
                config.LoadDefaultsFromTomlStream(stream);
            }
        });
    }
}
