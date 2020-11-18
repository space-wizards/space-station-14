using System.Linq;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces.GameObjects.Components;
using NUnit.Framework;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Hands
{
    [TestFixture]
    [TestOf(typeof(HandsComponent))]
    public class HandsTest : ContentIntegrationTest
    {
        private static readonly string HandsTestHandDummy = "HandsTestHandsDummy";
        private static readonly string HandsTestItemDummy = "HandsTestItemDummy";

        private static readonly string Prototypes = $@"
- type: entity
  name: {HandsTestHandDummy}
  id: {HandsTestHandDummy}
  components:
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso
  - type: Hands
  - type: Mind

- type: entity
  name: {HandsTestItemDummy}
  id: {HandsTestItemDummy}
  components:
  - type: Item
  - type: HandsTestHandEvent
";

        [Test]
        public async Task HandSelectedDeselectedTest()
        {
            var clientOptions = new ClientContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().Register<HandsTestHandEventComponent>();
                }
            };

            var serverOptions = new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().Register<HandsTestHandEventComponent>();
                }
            };

            var (client, server) = await StartConnectedServerClientPair(clientOptions, serverOptions);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            string firstHand = null;
            string secondHand = null;
            IEntity sHandsTestItemDummy = null;
            IItemComponent sItemComponent = null;
            IEntity sHandsTestHandDummy = null;
            Server.GameObjects.Components.GUI.HandsComponent sHandsComponent = null;
            HandsTestHandEventComponent sHandsEventComponent = null;

            var mapId = new MapId(1);
            var coordinates = new MapCoordinates(0, 0, mapId);

            await server.WaitPost(() =>
            {
                sHandsTestHandDummy = sEntityManager.SpawnEntity(HandsTestHandDummy, coordinates);
                sHandsComponent = sHandsTestHandDummy.GetComponent<Server.GameObjects.Components.GUI.HandsComponent>();
                firstHand = sHandsComponent.HandNames[0];
                secondHand = sHandsComponent.HandNames[1];

                sHandsTestItemDummy = sEntityManager.SpawnEntity(HandsTestItemDummy, coordinates);
                sHandsEventComponent = sHandsTestItemDummy.GetComponent<HandsTestHandEventComponent>();
                sItemComponent = sHandsTestItemDummy.GetComponent<IItemComponent>();

                var player = sPlayerManager.GetAllPlayers().Single();

                player.JoinGame();
                player.AttachedEntity!.Transform.WorldPosition = (0, 0);

                var playerMind = player.ContentData()!.Mind;
                var targetMind = sHandsTestHandDummy.GetComponent<MindComponent>();

                targetMind.Mind?.TransferTo(null);
                playerMind!.TransferTo(sHandsTestHandDummy);
            });

            await RunTicksSync(client, server, 5);
            await client.WaitRunTicks(1);

            var cEntityManager = client.ResolveDependency<IEntityManager>();
            IEntity cHandsTestHandDummy = null;
            IEntity cHandsTestItemDummy = null;
            SharedHandsComponent cHandsComponent = null;
            HandsTestHandEventComponent cHandsEventComponent = null;

            await client.WaitPost(() =>
            {
                cHandsTestHandDummy = cEntityManager.GetEntity(sHandsTestHandDummy.Uid);

                cHandsComponent = cHandsTestHandDummy.GetComponent<SharedHandsComponent>();

                cHandsTestItemDummy = cEntityManager.GetEntity(sHandsTestItemDummy.Uid);
                cHandsEventComponent = cHandsTestItemDummy.GetComponent<HandsTestHandEventComponent>();
            });

            cHandsEventComponent.AssertNone();
            sHandsEventComponent.AssertNone();

            await client.WaitAssertion(() =>
            {
                cHandsComponent.ActiveHand = null;
                Assert.Null(cHandsComponent.ActiveHand);
            });

            await server.WaitAssertion(() =>
            {
                sHandsComponent.ActiveHand = null;
                Assert.Null(sHandsComponent.ActiveHand);
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                cHandsEventComponent.AssertNone();
            });

            await server.WaitAssertion(() =>
            {
                sHandsEventComponent.AssertNone();
            });

            await client.WaitPost(() =>
            {
                Assert.That(cHandsComponent.HandNames[0], Is.EqualTo(firstHand));
            });

            await server.WaitAssertion(() =>
            {
                // Put item on a hand that is not active
                Assert.True(sHandsComponent.PutInHand(sItemComponent, false));
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                // No events since the hand that the item was put in is not active
                cHandsEventComponent.AssertNone();
            });

            await server.WaitAssertion(() =>
            {
                // No events since the hand that the item was put in is not active
                sHandsEventComponent.AssertNone();
            });

            await server.WaitAssertion(() =>
            {
                // Drop the item from the not active hand
                Assert.True(sHandsComponent.Drop(firstHand, false));
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                // No events since the hand that the item was dropped from is not active
                Assert.Zero(cHandsEventComponent.TimesHandsSelected);
                Assert.Zero(cHandsEventComponent.TimesHandsDeselected);
            });

            await server.WaitAssertion(() =>
            {
                // No events since the hand that the item was dropped from is not active
                Assert.Zero(sHandsEventComponent.TimesHandsSelected);
                Assert.Zero(sHandsEventComponent.TimesHandsDeselected);
            });

            await server.WaitAssertion(() =>
            {
                // Make the first hand active
                sHandsComponent.ActiveHand = firstHand;
                Assert.That(sHandsComponent.ActiveHand, Is.EqualTo(firstHand));
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                // That hand is now active on the client as well
                Assert.That(cHandsComponent.ActiveHand, Is.EqualTo(firstHand));
            });

            await server.WaitAssertion(() =>
            {
                // Pick up the item with the active hand
                Assert.True(sHandsComponent.PutInHand(sItemComponent, firstHand));
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                // 1 hand selected event on the client, from the item that was picked up in the active hand
                Assert.That(cHandsEventComponent.TimesHandsSelected, Is.EqualTo(1));
                Assert.Zero(cHandsEventComponent.TimesHandsDeselected);
            });

            await server.WaitAssertion(() =>
            {
                // 1 hand selected event on the server, from the item that was picked up in the active hand
                Assert.That(sHandsEventComponent.TimesHandsSelected, Is.EqualTo(1));
                Assert.Zero(sHandsEventComponent.TimesHandsDeselected);
            });

            cHandsEventComponent.ResetCounter();
            sHandsEventComponent.ResetCounter();

            await server.WaitPost(() =>
            {
                // Switch to the second hand on the server
                sHandsComponent.ActiveHand = secondHand;
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                Assert.Zero(cHandsEventComponent.TimesHandsSelected);

                // 1 hand deselected event on the client, from switching away from the item in hand
                Assert.That(cHandsEventComponent.TimesHandsDeselected, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                Assert.Zero(sHandsEventComponent.TimesHandsSelected);

                // 1 hand deselected event on the server, from switching away from the item in hand
                Assert.That(sHandsEventComponent.TimesHandsDeselected, Is.EqualTo(1));
            });

            cHandsEventComponent.ResetCounter();
            sHandsEventComponent.ResetCounter();

            await client.WaitPost(() =>
            {
                // Switch to the first hand on the client
                cHandsComponent.ActiveHand = firstHand;
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                // 2 hand selected events on the client, once from switching into the item in hand and once from syncing with the server
                Assert.That(cHandsEventComponent.TimesHandsSelected, Is.EqualTo(2));

                // 1 hand deselected event on the client, from syncing with the server
                Assert.That(cHandsEventComponent.TimesHandsDeselected, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                // 1 hand selected event on the server, from switching into the item in hand
                Assert.That(sHandsEventComponent.TimesHandsSelected, Is.EqualTo(1));
                Assert.Zero(sHandsEventComponent.TimesHandsDeselected);
            });

            cHandsEventComponent.ResetCounter();
            sHandsEventComponent.ResetCounter();

            await server.WaitAssertion(() =>
            {
                // Drop the item on the server
                Assert.True(sHandsComponent.Drop(firstHand, false));
            });

            await RunTicksSync(client, server, 5);
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            await client.WaitAssertion(() =>
            {
                Assert.Zero(cHandsEventComponent.TimesHandsSelected);

                // 1 hand deselected event on the client, from dropping the item in hand
                Assert.That(cHandsEventComponent.TimesHandsDeselected, Is.EqualTo(1));
            });

            await server.WaitAssertion(() =>
            {
                Assert.Zero(sHandsEventComponent.TimesHandsSelected);

                // 1 hand deselected event on the server, from dropping the item in hand
                Assert.That(sHandsEventComponent.TimesHandsDeselected, Is.EqualTo(1));
            });
        }

        private class HandsTestHandEventComponent : Component, IHandSelected, IHandDeselected
        {
            public override string Name => "HandsTestHandEvent";

            public int TimesHandsSelected { get; private set; }
            public int TimesHandsDeselected { get; private set; }

            public void ResetCounter()
            {
                TimesHandsSelected = 0;
                TimesHandsDeselected = 0;
            }

            public void AssertNone()
            {
                Assert.Zero(TimesHandsSelected);
                Assert.Zero(TimesHandsDeselected);
            }

            public void HandSelected(HandSelectedEventArgs eventArgs)
            {
                TimesHandsSelected++;
            }

            public void HandDeselected(HandDeselectedEventArgs eventArgs)
            {
                TimesHandsDeselected++;
            }
        }
    }
}
