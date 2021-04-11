using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using IPlayerManager = Robust.Server.Player.IPlayerManager;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Mobs
{
    [TestFixture]
    [TestOf(typeof(SharedActionsComponent))]
    [TestOf(typeof(ClientActionsComponent))]
    [TestOf(typeof(ServerActionsComponent))]
    [TestOf(typeof(ItemActionsComponent))]
    public class ActionsComponentTests : ContentIntegrationTest
    {
        const string Prototypes = @"
- type: entity
  name: flashlight
  parent: BaseItem
  id: TestFlashlight
  components:
    - type: HandheldLight
    - type: ItemActions
      actions:
        - actionType: ToggleLight
    - type: PowerCellSlot
    - type: Sprite
      sprite: Objects/Tools/flashlight.rsi
      layers:
        - state: lantern_off
        - state: HandheldLightOnOverlay
          shader: unshaded
          visible: false
    - type: Item
      sprite: Objects/Tools/flashlight.rsi
      HeldPrefix: off
    - type: PointLight
      enabled: false
      radius: 3
    - type: LoopingSound
    - type: Appearance
      visuals:
        - type: FlashLightVisualizer
";

        [Test]
        public async Task GrantsAndRevokesActionsTest()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var serverPlayerManager = server.ResolveDependency<IPlayerManager>();
            var innateActions = new List<ActionType>();

            await server.WaitAssertion(() =>
            {
                var player = serverPlayerManager.GetAllPlayers().Single();
                var playerEnt = player.AttachedEntity;
                var actionsComponent = playerEnt!.GetComponent<ServerActionsComponent>();

                // player should begin with their innate actions granted
                innateActions.AddRange(actionsComponent.InnateActions);
                foreach (var innateAction in actionsComponent.InnateActions)
                {
                    Assert.That(actionsComponent.TryGetActionState(innateAction, out var innateState));
                    Assert.That(innateState.Enabled);
                }
                Assert.That(innateActions.Count, Is.GreaterThan(0));

                actionsComponent.Grant(ActionType.DebugInstant);
                Assert.That(actionsComponent.TryGetActionState(ActionType.HumanScream, out var state) && state.Enabled);
            });

            // check that client has the actions
            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            var clientPlayerMgr = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
            var clientUIMgr = client.ResolveDependency<IUserInterfaceManager>();
            var expectedOrder = new List<ActionType>();

            await client.WaitAssertion(() =>
            {
                var local = clientPlayerMgr.LocalPlayer;
                var controlled = local!.ControlledEntity;
                var actionsComponent = controlled!.GetComponent<ClientActionsComponent>();

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
                var actionsComponent = playerEnt!.GetComponent<ServerActionsComponent>();
                actionsComponent.Revoke(ActionType.DebugInstant);
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            await client.WaitAssertion(() =>
            {
                var local = clientPlayerMgr.LocalPlayer;
                var controlled = local!.ControlledEntity;
                var actionsComponent = controlled!.GetComponent<ClientActionsComponent>();

                // we should have our innate actions, but debug1 should be revoked
                foreach (var innateAction in innateActions)
                {
                    Assert.That(actionsComponent.TryGetActionState(innateAction, out var innateState));
                    Assert.That(innateState.Enabled);
                }
                Assert.That(actionsComponent.TryGetActionState(ActionType.DebugInstant, out _), Is.False);

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
                        Assert.That(expected, Is.EqualTo(asAction.ActionType));

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
                    }
                }
            });
        }

        [Test]
        public async Task GrantsAndRevokesItemActions()
        {
            var serverOptions = new ServerIntegrationOptions { ExtraPrototypes = Prototypes };
            var clientOptions = new ClientIntegrationOptions { ExtraPrototypes = Prototypes };
            var (client, server) = await StartConnectedServerClientPair(serverOptions: serverOptions, clientOptions: clientOptions);

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var serverPlayerManager = server.ResolveDependency<IPlayerManager>();
            var serverEntManager = server.ResolveDependency<IEntityManager>();
            var serverGameTiming = server.ResolveDependency<IGameTiming>();

            var cooldown = Cooldowns.SecondsFromNow(30, serverGameTiming);

            ServerActionsComponent serverActionsComponent = null;
            ClientActionsComponent clientActionsComponent = null;
            IEntity serverPlayerEnt = null;
            IEntity serverFlashlight = null;

            await server.WaitAssertion(() =>
            {
                serverPlayerEnt = serverPlayerManager.GetAllPlayers().Single().AttachedEntity;
                serverActionsComponent = serverPlayerEnt!.GetComponent<ServerActionsComponent>();

                // spawn and give them an item that has actions
                serverFlashlight = serverEntManager.SpawnEntity("TestFlashlight",
                    new EntityCoordinates(new EntityUid(1), (0, 0)));
                Assert.That(serverFlashlight.TryGetComponent<ItemActionsComponent>(out var itemActions));
                // we expect this only to have a toggle light action initially
                var actionConfigs = itemActions.ActionConfigs.ToList();
                Assert.That(actionConfigs.Count == 1);
                Assert.That(actionConfigs[0].ActionType == ItemActionType.ToggleLight);
                Assert.That(actionConfigs[0].Enabled);

                // grant an extra item action, before pickup, initially disabled
                itemActions.GrantOrUpdate(ItemActionType.DebugToggle, false);
                serverPlayerEnt.GetComponent<HandsComponent>().PutInHand(serverFlashlight.GetComponent<ItemComponent>(), false);
                // grant an extra item action, after pickup, with a cooldown
                itemActions.GrantOrUpdate(ItemActionType.DebugInstant, cooldown: cooldown);

                Assert.That(serverActionsComponent.TryGetItemActionStates(serverFlashlight.Uid, out var state));
                // they should have been granted all 3 actions
                Assert.That(state.Count == 3);
                Assert.That(state.TryGetValue(ItemActionType.ToggleLight, out var toggleLightState));
                Assert.That(toggleLightState.Equals(new ActionState(true)));
                Assert.That(state.TryGetValue(ItemActionType.DebugInstant, out var debugInstantState));
                Assert.That(debugInstantState.Equals(new ActionState(true, cooldown: cooldown)));
                Assert.That(state.TryGetValue(ItemActionType.DebugToggle, out var debugToggleState));
                Assert.That(debugToggleState.Equals(new ActionState(false)));
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            // check that client has the actions, and toggle the light on via the action slot it was auto-assigned to
            var clientPlayerMgr = client.ResolveDependency<Robust.Client.Player.IPlayerManager>();
            var clientUIMgr = client.ResolveDependency<IUserInterfaceManager>();
            EntityUid clientFlashlight = default;
            await client.WaitAssertion(() =>
            {
                var local = clientPlayerMgr.LocalPlayer;
                var controlled = local!.ControlledEntity;
                clientActionsComponent = controlled!.GetComponent<ClientActionsComponent>();

                var lightEntry = clientActionsComponent.ItemActionStates()
                    .Where(entry => entry.Value.ContainsKey(ItemActionType.ToggleLight))
                    .FirstOrNull();
                clientFlashlight = lightEntry!.Value.Key;
                Assert.That(lightEntry, Is.Not.Null);
                Assert.That(lightEntry.Value.Value.TryGetValue(ItemActionType.ToggleLight, out var lightState));
                Assert.That(lightState.Equals(new ActionState(true)));
                Assert.That(lightEntry.Value.Value.TryGetValue(ItemActionType.DebugInstant, out var debugInstantState));
                Assert.That(debugInstantState.Equals(new ActionState(true, cooldown: cooldown)));
                Assert.That(lightEntry.Value.Value.TryGetValue(ItemActionType.DebugToggle, out var debugToggleState));
                Assert.That(debugToggleState.Equals(new ActionState(false)));

                var actionsUI = clientUIMgr.StateRoot.Children.FirstOrDefault(c => c is ActionsUI) as ActionsUI;
                Assert.That(actionsUI, Is.Not.Null);

                var toggleLightSlot = actionsUI.Slots.FirstOrDefault(slot => slot.Action is ItemActionPrototype
                {
                    ActionType: ItemActionType.ToggleLight
                });
                Assert.That(toggleLightSlot, Is.Not.Null);

                clientActionsComponent.AttemptAction(toggleLightSlot);
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            // server should see the action toggled on
            await server.WaitAssertion(() =>
            {
                Assert.That(serverActionsComponent.ItemActionStates().TryGetValue(serverFlashlight.Uid, out var lightStates));
                Assert.That(lightStates.TryGetValue(ItemActionType.ToggleLight, out var lightState));
                Assert.That(lightState, Is.EqualTo(new ActionState(true, toggledOn: true)));
            });

            // client should see it toggled on.
            await client.WaitAssertion(() =>
            {
                Assert.That(clientActionsComponent.ItemActionStates().TryGetValue(clientFlashlight, out var lightStates));
                Assert.That(lightStates.TryGetValue(ItemActionType.ToggleLight, out var lightState));
                Assert.That(lightState, Is.EqualTo(new ActionState(true, toggledOn: true)));
            });

            await server.WaitAssertion(() =>
            {
                // drop the item, and the item actions should go away
                serverPlayerEnt.GetComponent<HandsComponent>()
                    .TryDropEntity(serverFlashlight, serverPlayerEnt.Transform.Coordinates, false);
                Assert.That(serverActionsComponent.ItemActionStates().ContainsKey(serverFlashlight.Uid), Is.False);
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            // client should see they have no item actions for that item either.
            await client.WaitAssertion(() =>
            {
                Assert.That(clientActionsComponent.ItemActionStates().ContainsKey(clientFlashlight), Is.False);
            });

            await server.WaitAssertion(() =>
            {
                // pick the item up again, the states should be back to what they were when dropped,
                // as the states "stick" with the item
                serverPlayerEnt.GetComponent<HandsComponent>().PutInHand(serverFlashlight.GetComponent<ItemComponent>(), false);
                Assert.That(serverActionsComponent.ItemActionStates().TryGetValue(serverFlashlight.Uid, out var lightStates));
                Assert.That(lightStates.TryGetValue(ItemActionType.ToggleLight, out var lightState));
                Assert.That(lightState.Equals(new ActionState(true, toggledOn: true)));
                Assert.That(lightStates.TryGetValue(ItemActionType.DebugInstant, out var debugInstantState));
                Assert.That(debugInstantState.Equals(new ActionState(true, cooldown: cooldown)));
                Assert.That(lightStates.TryGetValue(ItemActionType.DebugToggle, out var debugToggleState));
                Assert.That(debugToggleState.Equals(new ActionState(false)));
            });

            await server.WaitRunTicks(5);
            await client.WaitRunTicks(5);

            // client should see the actions again, with their states back to what they were
            await client.WaitAssertion(() =>
            {
                Assert.That(clientActionsComponent.ItemActionStates().TryGetValue(clientFlashlight, out var lightStates));
                Assert.That(lightStates.TryGetValue(ItemActionType.ToggleLight, out var lightState));
                Assert.That(lightState.Equals(new ActionState(true, toggledOn: true)));
                Assert.That(lightStates.TryGetValue(ItemActionType.DebugInstant, out var debugInstantState));
                Assert.That(debugInstantState.Equals(new ActionState(true, cooldown: cooldown)));
                Assert.That(lightStates.TryGetValue(ItemActionType.DebugToggle, out var debugToggleState));
                Assert.That(debugToggleState.Equals(new ActionState(false)));
            });
        }
    }
}
