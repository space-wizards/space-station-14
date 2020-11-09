using System.Linq;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Server.GameObjects.Components.Mobs;
using NUnit.Framework;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(ClientAlertsComponent))]
    [TestOf(typeof(ServerAlertsComponent))]
    public class AlertsComponentTests : ContentIntegrationTest
    {

        [Test]
        public async Task AlertsTest()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var serverPlayerManager = server.ResolveDependency<Robust.Server.Interfaces.Player.IPlayerManager>();

            await server.WaitAssertion(() =>
            {
                var player = serverPlayerManager.GetAllPlayers().Single();
                var playerEnt = player.AttachedEntity;
                Assert.NotNull(playerEnt);
                var alertsComponent = playerEnt.GetComponent<ServerAlertsComponent>();
                Assert.NotNull(alertsComponent);

                // show 2 alerts
                alertsComponent.ShowAlert("debug1");
                alertsComponent.ShowAlert("debug2");
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            var clientPlayerMgr = client.ResolveDependency<IPlayerManager>();
            var clientUIMgr = client.ResolveDependency<IUserInterfaceManager>();
            await client.WaitAssertion(() =>
            {

                var local = clientPlayerMgr.LocalPlayer;
                Assert.NotNull(local);
                var controlled = local.ControlledEntity;
                Assert.NotNull(controlled);
                var alertsComponent = controlled.GetComponent<ClientAlertsComponent>();
                Assert.NotNull(alertsComponent);

                // find the alertsui
                var alertsUI =
                    clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is AlertsUI) as AlertsUI;
                Assert.NotNull(alertsUI);

                // we should be seeing 3 alerts - our health, and the 2 debug alerts, in a specific order.
                Assert.That(alertsUI.Grid.ChildCount, Is.EqualTo(3));
                var alertControls = alertsUI.Grid.Children.Select(c => c as AlertControl);
                var alertIDs = alertControls.Select(ac => ac.Alert.ID).ToArray();
                var expectedIDs = new [] {"humanhealth", "debug1", "debug2"};
                Assert.That(alertIDs, Is.EqualTo(expectedIDs));
            });

            await server.WaitAssertion(() =>
            {
                var player = serverPlayerManager.GetAllPlayers().Single();
                var playerEnt = player.AttachedEntity;
                Assert.NotNull(playerEnt);
                var alertsComponent = playerEnt.GetComponent<ServerAlertsComponent>();
                Assert.NotNull(alertsComponent);

                alertsComponent.ClearAlert("debug1");
            });
            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            await client.WaitAssertion(() =>
            {

                var local = clientPlayerMgr.LocalPlayer;
                Assert.NotNull(local);
                var controlled = local.ControlledEntity;
                Assert.NotNull(controlled);
                var alertsComponent = controlled.GetComponent<ClientAlertsComponent>();
                Assert.NotNull(alertsComponent);

                // find the alertsui
                var alertsUI =
                    clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is AlertsUI) as AlertsUI;
                Assert.NotNull(alertsUI);

                // we should be seeing only 2 alerts now because one was cleared
                Assert.That(alertsUI.Grid.ChildCount, Is.EqualTo(2));
                var alertControls = alertsUI.Grid.Children.Select(c => c as AlertControl);
                var alertIDs = alertControls.Select(ac => ac.Alert.ID).ToArray();
                var expectedIDs = new [] {"humanhealth", "debug2"};
                Assert.That(alertIDs, Is.EqualTo(expectedIDs));
            });
        }
    }
}
