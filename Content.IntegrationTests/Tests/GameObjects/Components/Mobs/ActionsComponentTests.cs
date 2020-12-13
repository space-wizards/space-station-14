using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using NUnit.Framework;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(SharedActionsComponent))]
    [TestOf(typeof(ClientActionsComponent))]
    [TestOf(typeof(ServerActionsComponent))]
    public class ActionsComponentTests : ContentIntegrationTest
    {
        [Test]
        public async Task GrantsAndRevokesActionsTest()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var serverPlayerManager = server.ResolveDependency<Robust.Server.Interfaces.Player.IPlayerManager>();
            var innateActions = new List<ActionType>();

            await server.WaitAssertion(() =>
            {
                var player = serverPlayerManager.GetAllPlayers().Single();
                var playerEnt = player.AttachedEntity;
                var actionsComponent = playerEnt.GetComponent<ServerActionsComponent>();

                // player should begin with their innate actions granted
                innateActions.AddRange(actionsComponent.InnateActions);
                foreach (var innateAction in actionsComponent.InnateActions)
                {
                    Assert.That(actionsComponent.TryGetActionState(innateAction, out var innateState));
                    Assert.That(innateState.Enabled);
                }

                actionsComponent.Grant(ActionType.DebugInstant);
                Assert.That(actionsComponent.TryGetActionState(ActionType.HumanScream, out var state) && state.Enabled);
            });

            // check that client has the actions
            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            var clientPlayerMgr = client.ResolveDependency<IPlayerManager>();
            var clientUIMgr = client.ResolveDependency<IUserInterfaceManager>();
            var expectedOrder = new List<ActionType>();
            await client.WaitAssertion(() =>
            {

                var local = clientPlayerMgr.LocalPlayer;
                var controlled = local.ControlledEntity;
                var actionsComponent = controlled.GetComponent<ClientActionsComponent>();

                // we should have our innate actions and debug1.
                foreach (var innateAction in innateActions)
                {
                    Assert.That(actionsComponent.TryGetActionState(innateAction, out var innateState));
                    Assert.That(innateState.Enabled);
                }
                Assert.That(actionsComponent.TryGetActionState(ActionType.DebugInstant, out var state) && state.Enabled);

                // innate actions should've auto-populated into our slots (in non-deterministic order),
                // but debug1 should be in the last slot
                var actionsUI =
                    clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is ActionsUI) as ActionsUI;
                Assert.That(actionsUI, Is.Not.Null);

                var expectedInnate = new HashSet<ActionType>(innateActions);
                var expectEmpty = false;
                expectedOrder.Clear();
                foreach (var slot in actionsUI.Slots)
                {
                    if (expectEmpty)
                    {
                        Assert.That(slot.HasAssignment, Is.False);
                        Assert.That(slot.Item, Is.Null);
                        Assert.That(slot.Action, Is.Null);
                        Assert.That(slot.ActionEnabled, Is.False);
                        continue;
                    }
                    Assert.That(slot.HasAssignment);
                    // all the actions we gave so far are not tied to an item
                    Assert.That(slot.Item, Is.Null);
                    Assert.That(slot.Action, Is.Not.Null);
                    Assert.That(slot.ActionEnabled);
                    var asAction = slot.Action as ActionPrototype;
                    Assert.That(asAction, Is.Not.Null);
                    expectedOrder.Add(asAction.ActionType);

                    if (expectedInnate.Count != 0)
                    {
                        Assert.That(expectedInnate.Remove(asAction.ActionType));
                    }
                    else
                    {
                        Assert.That(asAction.ActionType, Is.EqualTo(ActionType.DebugInstant));
                        Assert.That(slot.Cooldown, Is.Null);
                        expectEmpty = true;
                    }
                }
            });

            // now revoke the action and check that the client sees it as revoked
            await server.WaitAssertion(() =>
            {
                var player = serverPlayerManager.GetAllPlayers().Single();
                var playerEnt = player.AttachedEntity;
                var actionsComponent = playerEnt.GetComponent<ServerActionsComponent>();
                actionsComponent.Revoke(ActionType.DebugInstant);
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            await client.WaitAssertion(() =>
            {

                var local = clientPlayerMgr.LocalPlayer;
                var controlled = local.ControlledEntity;
                var actionsComponent = controlled.GetComponent<ClientActionsComponent>();

                // we should have our innate actions, but debug1 should be revoked
                foreach (var innateAction in innateActions)
                {
                    Assert.That(actionsComponent.TryGetActionState(innateAction, out var innateState));
                    Assert.That(innateState.Enabled);
                }
                Assert.That(actionsComponent.TryGetActionState(ActionType.DebugInstant, out var state), Is.False);

                // all actions should be in the same order as before, but the slot with DebugInstant should appear
                // disabled.
                var actionsUI =
                    clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is ActionsUI) as ActionsUI;
                Assert.That(actionsUI, Is.Not.Null);

                var idx = 0;
                foreach (var slot in actionsUI.Slots)
                {
                    if (idx < expectedOrder.Count)
                    {
                        var expected = expectedOrder[idx++];
                        Assert.That(slot.HasAssignment);
                        // all the actions we gave so far are not tied to an item
                        Assert.That(slot.Item, Is.Null);
                        Assert.That(slot.Action, Is.Not.Null);
                        var asAction = slot.Action as ActionPrototype;
                        Assert.That(asAction, Is.Not.Null);

                        if (asAction.ActionType == ActionType.DebugInstant)
                        {
                            Assert.That(slot.ActionEnabled, Is.False);
                        }
                        else
                        {
                            Assert.That(slot.ActionEnabled);
                        }
                    }
                    else
                    {
                        Assert.That(slot.HasAssignment, Is.False);
                        Assert.That(slot.Item, Is.Null);
                        Assert.That(slot.Action, Is.Null);
                        Assert.That(slot.ActionEnabled, Is.False);
                        continue;
                    }
                }
            });
        }

    }
}
