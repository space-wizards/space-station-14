using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
namespace Content.IntegrationTests.Tests.Lobby;

public sealed class ServerReloginTest
{
    [Test]
    public async Task Relogin()
    {
        IConfigurationManager serverConfig = default;
        IPlayerManager serverPlayerMgr = default;
        IClientNetManager clientNetManager = default;
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;
        var client = pairTracker.Pair.Client;
        int originalMaxPlayers = 0;
        string username = null;
        await server.WaitAssertion(() =>
        {
            serverConfig = IoCManager.Resolve<IConfigurationManager>();
            serverPlayerMgr = IoCManager.Resolve<IPlayerManager>();
            Assert.That(serverPlayerMgr.PlayerCount, Is.EqualTo(1));
            originalMaxPlayers = serverConfig.GetCVar(CCVars.SoftMaxPlayers);
            username = serverPlayerMgr.Sessions.First().Name;

            //No new players are allowed, but since our client was already playing, they should be able to get in
            serverConfig.SetCVar(CCVars.SoftMaxPlayers, 0);
        });

        await client.WaitAssertion(() =>
        {
            clientNetManager = IoCManager.Resolve<IClientNetManager>();
            clientNetManager.ClientDisconnect("For testing");
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 20);

        await server.WaitAssertion(() =>
        {
            Assert.That(serverPlayerMgr.PlayerCount, Is.EqualTo(0));
        });
        client.SetConnectTarget(server);
        await client.WaitPost(() =>
        {
            clientNetManager.ClientConnect(null!, 0, username);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 20);

        await server.WaitAssertion(() =>
        {

            // Check that we were able to reconnect
            Assert.That(serverPlayerMgr.PlayerCount, Is.EqualTo(1));

            //Put the cvar back, so other tests can still use this server
            serverConfig.SetCVar(CCVars.SoftMaxPlayers, originalMaxPlayers);
        });

        await pairTracker.CleanReturnAsync();
    }
}
