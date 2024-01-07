#nullable enable
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed partial class MindTests
{
    [Test]
    public async Task DeleteAllThenGhost()
    {
        var settings = new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true
        };
        await using var pair = await PoolManager.GetServerClient(settings);

        // Client is connected with a valid entity & mind
        Assert.That(pair.Client.EntMan.EntityExists(pair.Client.Player?.ControlledEntity));
        Assert.That(pair.Server.EntMan.EntityExists(pair.PlayerData?.Mind));

        // Delete **everything**
        var conHost = pair.Server.ResolveDependency<IConsoleHost>();
        await pair.Server.WaitPost(() => conHost.ExecuteCommand("entities delete"));
        await pair.RunTicksSync(5);

        Assert.That(pair.Server.EntMan.EntityCount, Is.EqualTo(0));
        Assert.That(pair.Client.EntMan.EntityCount, Is.EqualTo(0));

        // Create a new map.
        int mapId = 1;
        await pair.Server.WaitPost(() => conHost.ExecuteCommand($"addmap {mapId}"));
        await pair.RunTicksSync(5);

        // Client is not attached to anything
        Assert.Null(pair.Client.Player?.ControlledEntity);
        Assert.Null(pair.PlayerData?.Mind);

        // Attempt to ghost
        var cConHost = pair.Client.ResolveDependency<IConsoleHost>();
        await pair.Client.WaitPost(() => cConHost.ExecuteCommand("ghost"));
        await pair.RunTicksSync(10);

        // Client should be attached to a ghost placed on the new map.
        Assert.That(pair.Client.EntMan.EntityExists(pair.Client.Player?.ControlledEntity));
        Assert.That(pair.Server.EntMan.EntityExists(pair.PlayerData?.Mind));
        var xform = pair.Client.Transform(pair.Client.Player!.ControlledEntity!.Value);
        Assert.That(xform.MapID, Is.EqualTo(new MapId(mapId)));

        await pair.CleanReturnAsync();
    }
}
