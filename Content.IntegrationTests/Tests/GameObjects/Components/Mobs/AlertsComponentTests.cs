#nullable enable
using System.Linq;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Shared.Alert;
using Robust.Client.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs;

[TestOf(typeof(AlertsComponent))]
public sealed class AlertsComponentTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        DummyTicker = false
    };

    private static readonly ProtoId<AlertPrototype> Debug1 = "Debug1";
    private static readonly ProtoId<AlertPrototype> Debug2 = "Debug2";
    private static readonly ProtoId<AlertPrototype> HumanHealth = "HumanHealth";

    [SidedDependency(Side.Client)] private IUserInterfaceManager _cUIManager = default!;
    [SidedDependency(Side.Server)] private IPlayerManager _sPlayerManager = default!;
    [SidedDependency(Side.Server)] private AlertsSystem _sAlertsSystem = default!;

    [Test]
    public async Task AlertsTest()
    {
        EntityUid playerUid = default;
        await Server.WaitAssertion(() =>
        {
            playerUid = _sPlayerManager.Sessions.Single().AttachedEntity.GetValueOrDefault();
            Assert.That(playerUid, Is.Not.Default);
            // Making sure it exists
            Assert.That(playerUid, Has.Comp<AlertsComponent>(Server));

            var alerts = _sAlertsSystem.GetActiveAlerts(playerUid);
            Assert.That(alerts, Is.Not.Null);
            var alertCount = alerts.Count;

            _sAlertsSystem.ShowAlert(playerUid, Debug1);
            _sAlertsSystem.ShowAlert(playerUid, Debug2);

            Assert.That(alerts, Has.Count.EqualTo(alertCount + 2));
        });

        await RunTicksSync(5);

        AlertsUI? clientAlertsUI = default!;
        await Client.WaitAssertion(() =>
        {
            var local = Client.Session;
            Assert.That(local, Is.Not.Null);
            var controlled = local.AttachedEntity;
            Assert.That(controlled, Is.Not.Null);
            // Making sure it exists
            Assert.That(controlled, Has.Comp<AlertsComponent>(Server));

            // find the alertsui
            Assert.That(_cUIManager.ActiveScreen, Is.Not.Null);
            clientAlertsUI = FindAlertsUI(_cUIManager.ActiveScreen);
            Assert.That(clientAlertsUI, Is.Not.Null);

            static AlertsUI? FindAlertsUI(Control control)
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
            var alertControls = clientAlertsUI.AlertContainer.Children.Select(c => (AlertControl)c);
            var alertIDs = alertControls.Select(ac => ac.Alert.ID).ToArray();
            var expectedIDs = new[] { HumanHealth, Debug1, Debug2 };
            Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
        });

        await Server.WaitAssertion(() =>
        {
            _sAlertsSystem.ClearAlert(playerUid, Debug1);
        });

        await RunTicksSync(5);

        await Client.WaitAssertion(() =>
        {
            // we should be seeing 2 alerts now because one was cleared
            Assert.That(clientAlertsUI.AlertContainer.ChildCount, Is.GreaterThanOrEqualTo(2));
            var alertControls = clientAlertsUI.AlertContainer.Children.Select(c => (AlertControl)c);
            var alertIDs = alertControls.Select(ac => ac.Alert.ID).ToArray();
            var expectedIDs = new[] { HumanHealth, Debug2 };
            Assert.That(alertIDs, Is.SupersetOf(expectedIDs));
        });
    }
}
