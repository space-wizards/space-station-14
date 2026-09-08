using Content.IntegrationTests.Fixtures;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Networking;

[TestFixture]
public sealed class PvsCommandTest : GameTest
{
    private static readonly EntProtoId TestEnt = "MobHuman";

    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false };

    [Test]
    public async Task TestPvsCommands()
    {
        var pair = Pair;
        var (server, client) = pair;

        // Spawn a complex entity.
        EntityUid entity = default;
        await server.WaitPost(() => entity = server.EntMan.Spawn(TestEnt));
        await pair.RunTicksSync(5);

        // Check that the client has a variety pf entities.
        Assert.That(client.EntMan.EntityCount, Is.GreaterThan(0));
        Assert.That(client.EntMan.Count<MapComponent>, Is.GreaterThan(0));
        Assert.That(client.EntMan.Count<MapGridComponent>, Is.GreaterThan(0));

        var meta = client.MetaData(pair.ToClientUid(entity));
        var lastApplied = meta.LastStateApplied;

        // Dirty all entities
        await server.ExecuteCommand("dirty");
        await pair.RunTicksSync(5);
        Assert.That(meta.LastStateApplied, Is.GreaterThan(lastApplied));
        await pair.RunTicksSync(5);

        // Do a client-side full state reset
        await client.ExecuteCommand("resetallents");
        await pair.RunTicksSync(5);

        // Request a full server state.
        lastApplied = meta.LastStateApplied;
        await client.ExecuteCommand("fullstatereset");
        await pair.RunTicksSync(10);
        Assert.That(meta.LastStateApplied, Is.GreaterThan(lastApplied));

        await server.WaitPost(() => server.EntMan.DeleteEntity(entity));
    }
}
