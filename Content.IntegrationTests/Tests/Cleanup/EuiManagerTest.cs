#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration.UI;
using Content.Server.EUI;
using Robust.Server.Player;

namespace Content.IntegrationTests.Tests.Cleanup;

public sealed class EuiManagerTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        Dirty = true
    };

    [SidedDependency(Side.Server)] private IPlayerManager _sPlayerManager = null!;
    [SidedDependency(Side.Server)] private EuiManager _euiManager = null!;

    [Test]
    [Retry(2)]
    // Even though we are using the server EUI here, we actually want to see if the client EUIManager crashes
    public async Task EuiManagerRecycleWithOpenWindowTest()
    {
        await Server.WaitAssertion(() =>
        {
            var clientSession = _sPlayerManager.Sessions.Single();
            var ui = new AdminAnnounceEui();
            _euiManager.OpenEui(ui, clientSession);
        });

        await RunUntilSynced();
    }
}
