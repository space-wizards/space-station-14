using System.Linq;
using System.Threading.Tasks;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(AlertsComponent))]
    public sealed class AlertsComponentTests
    {
        [Test]
        public async Task AlertsTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            var serverPlayerManager = server.ResolveDependency<IPlayerManager>();
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();

            EntityUid playerUid = default;
            await server.WaitAssertion(() =>
            {
                playerUid = serverPlayerManager.Sessions.Single().AttachedEntity.GetValueOrDefault();
                Assert.That(playerUid != default);
                // Making sure it exists
                _ = IoCManager.Resolve<IEntityManager>().GetComponent<AlertsComponent>(playerUid);

                var alerts = alertsSystem.GetActiveAlerts(playerUid);
                Assert.IsNotNull(alerts);
                var alertCount = alerts.Count;

                alertsSystem.ShowAlert(playerUid, AlertType.Debug1);
                alertsSystem.ShowAlert(playerUid, AlertType.Debug2);

                Assert.That(alerts, Has.Count.EqualTo(alertCount + 2));
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            AlertsUI clientAlertsUI = default;
            await client.WaitAssertion(() =>
            {
                var clientPlayerMgr = IoCManager.Resolve<Robust.Client.Player.IPlayerManager>();
                var clientUIMgr = IoCManager.Resolve<IUserInterfaceManager>();

                var local = clientPlayerMgr.LocalPlayer;
                Assert.NotNull(local);
                var controlled = local.ControlledEntity;
                Assert.NotNull(controlled);
                // Making sure it exists
                _ = IoCManager.Resolve<IEntityManager>().GetComponent<AlertsComponent>(controlled.Value);

                // find the alertsui

                clientAlertsUI = FindAlertsUI(clientUIMgr.ActiveScreen);
                Assert.NotNull(clientAlertsUI);

                AlertsUI FindAlertsUI(Control control)
                {
                    if (control is AlertsUI alertUI)
                        return alertUI;
                    foreach (var child in control.Children)
                    {
                        var found = FindAlertsUI(child);
                        if (found != null)
                            return found;
                    }

                    return null;
                }

                // we should be seeing 3 alerts - our health, and the 2 debug alerts, in a specific order.
                Assert.That(clientAlertsUI.AlertContainer.ChildCount, Is.GreaterThanOrEqualTo(3));
                var alertControls = clientAlertsUI.AlertContainer.Children.Select(c => (AlertControl) c);
                var alertIDs = alertControls.Select(ac => ac.Alert.AlertType).ToArray();
                var expectedIDs = new [] {AlertType.HumanHealth, AlertType.Debug1, AlertType.Debug2};
                Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
            });

            await server.WaitAssertion(() =>
            {
                alertsSystem.ClearAlert(playerUid, AlertType.Debug1);
            });

            await PoolManager.RunTicksSync(pairTracker.Pair, 5);

            await client.WaitAssertion(() =>
            {
                // we should be seeing 2 alerts now because one was cleared
                Assert.That(clientAlertsUI.AlertContainer.ChildCount, Is.GreaterThanOrEqualTo(2));
                var alertControls = clientAlertsUI.AlertContainer.Children.Select(c => (AlertControl) c);
                var alertIDs = alertControls.Select(ac => ac.Alert.AlertType).ToArray();
                var expectedIDs = new [] {AlertType.HumanHealth, AlertType.Debug2};
                Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
