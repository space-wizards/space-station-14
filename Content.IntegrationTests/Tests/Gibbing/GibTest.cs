#nullable enable
using Content.Shared.Gibbing;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Body;

[TestFixture]
public sealed class GibTest
{
    [Test]
    public async Task TestGib()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var (server, client) = (pair.Server, pair.Client);
        var map = await pair.CreateTestMap();

        EntityUid target = default;

        await server.WaitAssertion(() => target = server.EntMan.Spawn("MobHuman", map.MapCoords));
        await pair.WaitCommand($"setoutfit {server.EntMan.GetNetEntity(target)} CaptainGear");

        await pair.RunTicksSync(5);
        var nuid = pair.ToClientUid(target);
        Assert.That(client.EntMan.EntityExists(nuid));

        await server.WaitAssertion(() => server.System<GibbingSystem>().Gib(target));

        await pair.RunTicksSync(5);
        await pair.WaitCommand("dirty");
        await pair.RunTicksSync(5);

        Assert.That(!client.EntMan.EntityExists(nuid));

        await pair.CleanReturnAsync();
    }
}
