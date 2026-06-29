using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.UI;
using Content.Server.EUI;
using Robust.Server.Player;

namespace Content.IntegrationTests.Tests.Cleanup;

public sealed class EuiManagerTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings
    {
        Connected = true,
        Dirty = true
    };

    [Test]
    [Retry(2)]
    // Even though we are using the server EUI here, we actually want to see if the client EUIManager crashes
    public async Task EuiManagerRecycleWithOpenWindowTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var sPlayerManager = server.ResolveDependency<IPlayerManager>();
        var eui = server.ResolveDependency<EuiManager>();

        await server.WaitAssertion(() =>
        {
            var clientSession = sPlayerManager.Sessions.Single();
            var ui = new AdminAnnounceEui();
            eui.OpenEui(ui, clientSession);
        });

        await RunUntilSynced();
    }
}
