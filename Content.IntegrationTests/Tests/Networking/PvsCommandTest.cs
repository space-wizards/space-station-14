#nullable enable
using Content.IntegrationTests.Fixtures;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Networking;

public sealed class PvsCommandTest : GameTest
{
    private static readonly EntProtoId TestEnt = "MobHuman";

    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        DummyTicker = false,
    };

    [Test]
    public async Task TestPvsCommands()
    {
        // Spawn a complex entity.
        var entity = await Spawn(TestEnt);
        await RunUntilSynced();

        using (Assert.EnterMultipleScope())
        {
            // Check that the client has a variety of entities.
            Assert.That(CEntMan.EntityCount, Is.GreaterThan(0));
            Assert.That(CEntMan.Count<MapComponent>(), Is.GreaterThan(0));
            Assert.That(CEntMan.Count<MapGridComponent>(), Is.GreaterThan(0));
        }

        var meta = Client.MetaData(ToClientUid(entity));
        var lastApplied = meta.LastStateApplied;

        // Dirty all entities
        await Server.ExecuteCommand("dirty");
        await RunUntilSynced();
        Assert.That(meta.LastStateApplied, Is.GreaterThan(lastApplied));
        await RunUntilSynced();

        // Do a client-side full state reset
        await Client.ExecuteCommand("resetallents");
        await RunUntilSynced();

        // Request a full server state.
        lastApplied = meta.LastStateApplied;
        await Client.ExecuteCommand("fullstatereset");
        await RunUntilSynced();
        Assert.That(meta.LastStateApplied, Is.GreaterThan(lastApplied));
    }
}
