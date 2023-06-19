using System.Linq;
using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class GhostTests
{
    [Test]
    public async Task TestPlayerCanGhostThenDisconnectAndReconnect()
    {
        // Client is needed to spawn session
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;
        var client = pairTracker.Pair.Client;

        var netManager = client.ResolveDependency<IClientNetManager>();

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        EntityUid entity = default!;
        Mind mind = default!;
        IPlayerSession player = playerMan.ServerSessions.Single();

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity != null);
            entity = player.AttachedEntity.Value;
            Assert.That(entMan.TryGetComponent(entity, out MindContainerComponent mindContainerComponent));
            Assert.That(mindContainerComponent.HasMind);
            mind = mindContainerComponent.Mind;
            entMan.DeleteEntity(entity);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        EntityUid mob = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(mind.OwnedEntity != null);
            Assert.That(entity != mind.OwnedEntity);
            mob = mind.OwnedEntity.Value;

        });

        await client.WaitAssertion(() =>
        {
            netManager.ClientDisconnect("Disconnect command used.");
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        client.SetConnectTarget(server);
        await client.WaitPost(() => netManager.ClientConnect(null!, 0, null!));
        await PoolManager.RunTicksSync(pairTracker.Pair, 10);

        await server.WaitAssertion(() =>
        {
            // New ghost is created to attach old mind to.
            // Make sure that session is set correctly
            // Mind still exists
            var m = player.ContentData()?.Mind;
            Assert.That(m, Is.Not.EqualTo(null));

            Assert.That(m!.OwnedEntity, Is.Not.EqualTo(mob));
            Assert.That(m, Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }
}
