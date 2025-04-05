using System.Linq;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Shared.Alert;
using Robust.Client.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(AlertsComponent))]
    public sealed class AlertsComponentTests
    {
        [Test]
        public async Task AlertsTest()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                Connected = true,
                DummyTicker = false
            });
            var server = pair.Server;
            var client = pair.Client;

            var clientUIMgr = client.ResolveDependency<IUserInterfaceManager>();
            var clientEntManager = client.ResolveDependency<IEntityManager>();

            var entManager = server.ResolveDependency<IEntityManager>();
            var serverPlayerManager = server.ResolveDependency<IPlayerManager>();
            var alertsSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<AlertsSystem>();

            EntityUid playerUid = default;
            await server.WaitAssertion(() =>
            {
                playerUid = serverPlayerManager.Sessions.Single().AttachedEntity.GetValueOrDefault();
#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.That(playerUid, Is.Not.EqualTo(default(EntityUid)));
                // Making sure it exists
                Assert.That(entManager.HasComponent<AlertsComponent>(playerUid));
#pragma warning restore NUnit2045

                var alerts = alertsSystem.GetActiveAlerts(playerUid);
                Assert.That(alerts, Is.Not.Null);
                var alertCount = alerts.Count;

                alertsSystem.ShowAlert(playerUid, "Debug1");
                alertsSystem.ShowAlert(playerUid, "Debug2");

                Assert.That(alerts, Has.Count.EqualTo(alertCount + 2));
            });

            await pair.RunTicksSync(5);

            AlertsUI clientAlertsUI = default;
            await client.WaitAssertion(() =>
            {
                var local = client.Session;
                Assert.That(local, Is.Not.Null);
                var controlled = local.AttachedEntity;
#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.That(controlled, Is.Not.Null);
                // Making sure it exists
                Assert.That(clientEntManager.HasComponent<AlertsComponent>(controlled.Value));
#pragma warning restore Nunit2045

                // find the alertsui

                clientAlertsUI = FindAlertsUI(clientUIMgr.ActiveScreen);
                Assert.That(clientAlertsUI, Is.Not.Null);

                static AlertsUI FindAlertsUI(Control control)
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
                var alertIDs = alertControls.Select(ac => ac.Alert.ID).ToArray();
                var expectedIDs = new[] { "HumanHealth", "Debug1", "Debug2" };
                Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
            });

            await server.WaitAssertion(() =>
            {
                alertsSystem.ClearAlert(playerUid, "Debug1");
            });

            await pair.RunTicksSync(5);

            await client.WaitAssertion(() =>
            {
                // we should be seeing 2 alerts now because one was cleared
                Assert.That(clientAlertsUI.AlertContainer.ChildCount, Is.GreaterThanOrEqualTo(2));
                var alertControls = clientAlertsUI.AlertContainer.Children.Select(c => (AlertControl) c);
                var alertIDs = alertControls.Select(ac => ac.Alert.ID).ToArray();
                var expectedIDs = new[] { "HumanHealth", "Debug2" };
                Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
            });

            await pair.CleanReturnAsync();
        }

        /// <summary>
        /// Test that client-sided alerts function properly.
        /// </summary>
        [Test]
        public async Task ClientAlertsTest()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                Connected = true,
                DummyTicker = false,
                Fresh = true, // I don't know why this doesn't work without it, but it doesn't work without it.
            });
            var server = pair.Server;
            var client = pair.Client;

            var clientEntityManager = client.ResolveDependency<IEntityManager>();
            var clientAlertsSystem = clientEntityManager.System<AlertsSystem>();

            var serverEntityManager = server.ResolveDependency<IEntityManager>();
            var serverAlertsSystem = serverEntityManager.System<AlertsSystem>();

            EntityUid playerUid = default;
            await client.WaitAssertion(() =>
            {
                playerUid = client.AttachedEntity.GetValueOrDefault();
                Assert.That(playerUid, Is.Not.EqualTo(default(EntityUid)));

                clientAlertsSystem.ShowAlert(playerUid, "DebugClient");
            });

            await pair.RunTicksSync(5);

            await client.WaitAssertion(() =>
            {
                var expectedAlerts = new[] { "HumanHealth", "DebugClient" };
                Assert.That(clientAlertsSystem.GetActiveAlerts(playerUid), Is.Not.Null);
                Assert.That(
                    clientAlertsSystem.GetActiveAlerts(playerUid)!
                        .Select(key => key.Key.AlertType.GetValueOrDefault("Debug7").Id),
                    Is.SupersetOf(expectedAlerts));
            });

            await server.WaitAssertion(() =>
            {
                serverAlertsSystem.ShowAlert(playerUid, "Debug1");
            });

            await pair.RunTicksSync(5);

            await client.WaitAssertion(() =>
            {
                var expectedAlerts = new[] { "HumanHealth", "DebugClient", "Debug1" };
                Assert.That(clientAlertsSystem.GetActiveAlerts(playerUid), Is.Not.Null);
                Assert.That(
                    clientAlertsSystem.GetActiveAlerts(playerUid)!
                        .Select(key => key.Key.AlertType.GetValueOrDefault("Debug7").Id),
                    Is.SupersetOf(expectedAlerts));
            });

            await pair.CleanReturnAsync();
        }
    }
}
