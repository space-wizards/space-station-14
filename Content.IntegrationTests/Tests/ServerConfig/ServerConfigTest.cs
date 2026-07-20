#nullable enable
using System.Collections.Generic;
using System.Text;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.IntegrationTests.Tests.ServerConfig;

/// <summary>
/// A test fixture to check interaction between server configurations
/// </summary>
[TestFixture]
public sealed class ServerConfigTest : GameTest
{
    // Config file paths, all relative to resource directory.
    const string DefaultConfig = "/ConfigPresets/server_config.toml";
    const string ToolsConfig = "/ConfigPresets/Build/development.toml";
    const string DebugConfig = "/ConfigPresets/Build/debug.toml";

    public override PoolSettings PoolSettings => new()
    {
        InLobby = true
    };

    /// <summary>
    /// A test that checks that the default server config TOML file does not overlap with dev config.
    /// If it did, it would hide the dev config.
    /// </summary>
    [Test]
    public async Task CheckDefaultConfigOverlapTest()
    {
        var cfg = Pair.Server.ResolveDependency<IConfigurationManager>();
        var res = Pair.Server.ResolveDependency<IResourceManager>();

        // Try to read cvars from various config files.
        Assert.That(res.TryContentFileRead(DefaultConfig, out var stream), $"Could not read default config at {DefaultConfig}");
        var configCvars = cfg.ValidateTomlStream(stream!);
        Assert.That(res.TryContentFileRead(ToolsConfig, out stream), $"Could not read tools config at {ToolsConfig}");
        var toolsCvars = cfg.ValidateTomlStream(stream!);
        Assert.That(res.TryContentFileRead(DebugConfig, out stream), $"Could not read dev config at {DebugConfig}");
        var devCvars = cfg.ValidateTomlStream(stream!);

        // Check whether or not cvars overlap.
        Assert.That(CheckOverlap(configCvars, toolsCvars, out var overlappingCvars), $"Overlapping cvars between {DefaultConfig} and {ToolsConfig}: {overlappingCvars}");
        Assert.That(CheckOverlap(configCvars, devCvars, out overlappingCvars), $"Overlapping cvars between {DefaultConfig} and {DebugConfig}: {overlappingCvars}");
        Assert.That(CheckOverlap(devCvars, toolsCvars, out overlappingCvars), $"Overlapping cvars between {DebugConfig} and {ToolsConfig}: {overlappingCvars}");
    }

    private bool CheckOverlap(HashSet<string> firstSet, HashSet<string> secondSet, out string overlapNames)
    {
        StringBuilder outNames = new();
        var ret = true;
        foreach (var value in firstSet)
        {
            if (secondSet.Contains(value))
            {
                ret = false;
                if (outNames.Length > 0)
                    outNames.Append(", ");
                outNames.Append(value);
            }
        }
        overlapNames = outNames.ToString();
        return ret;
    }
}
