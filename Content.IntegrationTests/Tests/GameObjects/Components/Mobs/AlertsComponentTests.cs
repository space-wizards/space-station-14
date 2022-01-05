using System.Linq;
using System.Threading.Tasks;
using Content.Client.Alerts.UI;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Server.Player;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(AlertsComponent))]
    public class AlertsComponentTests : ContentIntegrationTest
    {
        [Test]
        public async Task AlertsTest()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var serverPlayerManager = server.ResolveDependency<IPlayerManager>();
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();

            await server.WaitAssertion(() =>
            {
                var playerEnt = serverPlayerManager.Sessions.Single().AttachedEntity.GetValueOrDefault();
                Assert.That(playerEnt != default);
                var alertsComponent = IoCManager.Resolve<IEntityManager>().GetComponent<AlertsComponent>(playerEnt);
                Assert.NotNull(alertsComponent);

                // show 2 alerts
                alertsSystem.ShowAlert(alertsComponent.Owner, AlertType.Debug1, null, null);
                alertsSystem.ShowAlert(alertsComponent.Owner, AlertType.Debug2, null, null);
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            var clientPlayerMgr = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
            var clientUIMgr = client.ResolveDependency<IUserInterfaceManager>();
            await client.WaitAssertion(() =>
            {

                var local = clientPlayerMgr.LocalPlayer;
                Assert.NotNull(local);
                var controlled = local.ControlledEntity;
                Assert.NotNull(controlled);
                var alertsComponent = IoCManager.Resolve<IEntityManager>().GetComponent<AlertsComponent>(controlled.Value);
                Assert.NotNull(alertsComponent);

                // find the alertsui
                var alertsUI =
                    clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is AlertsUI) as AlertsUI;
                Assert.NotNull(alertsUI);

                // we should be seeing 3 alerts - our health, and the 2 debug alerts, in a specific order.
                Assert.That(alertsUI.AlertContainer.ChildCount, Is.GreaterThanOrEqualTo(3));
                var alertControls = alertsUI.AlertContainer.Children.Select(c => (AlertControl) c);
                var alertIDs = alertControls.Select(ac => ac.Alert.AlertType).ToArray();
                var expectedIDs = new [] {AlertType.HumanHealth, AlertType.Debug1, AlertType.Debug2};
                Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
            });

            await server.WaitAssertion(() =>
            {
                var playerEnt = serverPlayerManager.Sessions.Single().AttachedEntity.GetValueOrDefault();
                Assert.That(playerEnt, Is.Not.EqualTo(default));
                var alertsComponent = IoCManager.Resolve<IEntityManager>().GetComponent<AlertsComponent>(playerEnt);
                Assert.NotNull(alertsComponent);

                alertsSystem.ClearAlert(alertsComponent.Owner, AlertType.Debug1);
            });
            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            await client.WaitAssertion(() =>
            {

                var local = clientPlayerMgr.LocalPlayer;
                Assert.NotNull(local);
                var controlled = local.ControlledEntity;
                Assert.NotNull(controlled);
                var alertsComponent = IoCManager.Resolve<IEntityManager>().GetComponent<AlertsComponent>(controlled.Value);
                Assert.NotNull(alertsComponent);

                // find the alertsui
                var alertsUI =
                    clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is AlertsUI) as AlertsUI;
                Assert.NotNull(alertsUI);

                // we should be seeing 2 alerts now because one was cleared
                Assert.That(alertsUI.AlertContainer.ChildCount, Is.GreaterThanOrEqualTo(2));
                var alertControls = alertsUI.AlertContainer.Children.Select(c => (AlertControl) c);
                var alertIDs = alertControls.Select(ac => ac.Alert.AlertType).ToArray();
                var expectedIDs = new [] {AlertType.HumanHealth, AlertType.Debug2};
                Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
            });
        }
    }
}
