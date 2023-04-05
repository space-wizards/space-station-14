using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.UI;
using Content.Server.EUI;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.Cleanup;

public sealed class EuiManagerTest
{
    [Test]
    public async Task EuiManagerRecycleWithOpenWindowTest()
    {
        // Even though we are using the server EUI here, we actually want to see if the client EUIManager crashes
        for (int i = 0; i < 2; i++)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{Dirty = true});
            var server = pairTracker.Pair.Server;

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            await server.WaitAssertion(() =>
            {
                var clientSession = sPlayerManager.ServerSessions.Single();
                var eui = IoCManager.Resolve<EuiManager>();
                var ui = new AdminAnnounceEui();
                eui.OpenEui(ui, clientSession);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
