using System.Linq;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Alert;
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
                alertsComponent.ShowAlert(AlertType.Debug1);
                alertsComponent.ShowAlert(AlertType.Debug2);
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
                var alertIDs = alertControls.Select(ac => ac.Alert.AlertType).ToArray();
                var expectedIDs = new [] {AlertType.HumanHealth, AlertType.Debug1, AlertType.Debug2};
                Assert.That(alertIDs, Is.EqualTo(expectedIDs));
            });

            await server.WaitAssertion(() =>
            {
                var player = serverPlayerManager.GetAllPlayers().Single();
                var playerEnt = player.AttachedEntity;
                Assert.NotNull(playerEnt);
                var alertsComponent = playerEnt.GetComponent<ServerAlertsComponent>();
                Assert.NotNull(alertsComponent);

                alertsComponent.ClearAlert(AlertType.Debug1);
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
                var alertIDs = alertControls.Select(ac => ac.Alert.AlertType).ToArray();
                var expectedIDs = new [] {AlertType.HumanHealth, AlertType.Debug2};
                Assert.That(alertIDs, Is.EqualTo(expectedIDs));
            });
        }
    }
}
