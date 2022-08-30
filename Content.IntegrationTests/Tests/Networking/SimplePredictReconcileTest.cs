#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Timing;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Networking
{
    // This test checks that the prediction & reconciling system is working correctly with a simple boolean flag.
    // An entity system sets a flag on a networked component via a RaisePredictiveEvent,
    // so it runs predicted on client and eventually on server.
    // All the tick values are checked to ensure it arrives on client & server at the exact correct ticks.
    // On the client, the reconciling system is checked to ensure that the state correctly reset every tick,
    // until the server acknowledges it.
    // Then, the same test is performed again, but the server does not handle the message (it ignores it).
    // To simulate a mispredict.
    // This means the client is forced to reset it once it gets to the server tick where the server didn't do anything.
    // the tick where the server *should* have, but did not, acknowledge the state change.
    // Finally, we run two events inside the prediction area to ensure reconciling does for incremental stuff.
    [TestFixture]
    public sealed class SimplePredictReconcileTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new (){Fresh = true, DummyTicker = true});
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            // Pull in all dependencies we need.
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var cEntityManager = client.ResolveDependency<IEntityManager>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();
            var cGameTiming = client.ResolveDependency<IClientGameTiming>();
            var cGameStateManager = client.ResolveDependency<IClientGameStateManager>();
            var cfg = client.ResolveDependency<IConfigurationManager>();
            var log = cfg.GetCVar(CVars.NetLogging);

            //cfg.SetCVar(CVars.NetLogging, true);

            EntityUid serverEnt = default;
            PredictionTestComponent serverComponent = default!;
            PredictionTestComponent clientComponent = default!;

            var serverSystem = server.ResolveDependency<IEntitySystemManager>()
                .GetEntitySystem<PredictionTestEntitySystem>();
            var clientSystem = client.ResolveDependency<IEntitySystemManager>()
                .GetEntitySystem<PredictionTestEntitySystem>();

            await server.WaitPost(() =>
            {
                // Spawn dummy component entity.
                var map = sMapManager.CreateMap();
                var player = sPlayerManager.ServerSessions.Single();
                serverEnt = sEntityManager.SpawnEntity(null, new MapCoordinates((0, 0), map));
                serverComponent = IoCManager.Resolve<IEntityManager>().AddComponent<PredictionTestComponent>(serverEnt);

                // Make client "join game" so they receive game state updates.
                player.JoinGame();
            });

            // Run some ticks so that
            await PoolManager.RunTicksSync(pairTracker.Pair, 3);

            // Check client buffer is full
            Assert.That(cGameStateManager.CurrentBufferSize, Is.EqualTo(cGameStateManager.TargetBufferSize));

            // This isn't required anymore, but the test had this for the sake of "technical things", and I cbf shifting
            // all the tick times over. So it stays.
            await client.WaitRunTicks(1);

            await client.WaitPost(() =>
            {
                clientComponent = cEntityManager.GetComponent<PredictionTestComponent>(serverEnt);
            });

            Assert.That(clientComponent.Foo, Is.False);

            // KEEP IN MIND WHEN READING THIS.
            // The game loop increments CurTick AFTER running the tick.
            // So when reading CurTick inside an Assert or Post or whatever, the tick reported is the NEXT one to run.

            Assert.That(sGameTiming.CurTick, Is.EqualTo(new GameTick(14)));
            Assert.That(serverComponent.Foo, Is.False);

            // Client last ran tick 15 meaning it's ahead of the last server tick it processed (12)
            Assert.That(cGameTiming.CurTick, Is.EqualTo(new GameTick(16)));
            Assert.That(cGameTiming.LastProcessedTick, Is.EqualTo(new GameTick(12)));

            // *** I am using block scopes to visually distinguish these sections of the test to make it more readable.


            // Send an event to change the flag and instantly see the effect replicate client side,
            // while it's queued on server and reconciling works (constantly needs re-firing on client).
            {
                await client.WaitPost(() =>
                {
                    cEntityManager.RaisePredictiveEvent(new SetFooMessage(serverEnt, true));

                    Assert.That(clientComponent.Foo, Is.True);
                });

                // Event correctly arrived on client system.
                Assert.That(clientSystem.EventTriggerList,
                    Is.EquivalentTo(new[] {(new GameTick(16), true, false, true, true)}));
                clientSystem.EventTriggerList.Clear();

                // Two ticks happen on both sides with nothing really "changing".
                // Server doesn't receive it yet,
                // client is still replaying the past prediction.
                for (var i = 0; i < 2; i++)
                {
                    await server.WaitRunTicks(1);

                    // Event did not arrive on server.
                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(16), false, false, true, true)}));
                    clientSystem.EventTriggerList.Clear();
                }

                {
                    await server.WaitRunTicks(1);

                    // Event arrived on server at tick 16.
                    Assert.That(sGameTiming.CurTick, Is.EqualTo(new GameTick(17)));
                    Assert.That(serverSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(16), true, false, true, true)}));
                    serverSystem.EventTriggerList.Clear();

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(16), false, false, true, true)}));
                    clientSystem.EventTriggerList.Clear();
                }

                {
                    await server.WaitRunTicks(1);

                    // Nothing happened on server.
                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList, Is.Empty);
                    Assert.That(clientComponent.Foo, Is.True);
                    clientSystem.EventTriggerList.Clear();
                }
            }

            // Disallow changes to simulate a misprediction.
            serverSystem.Allow = false;

            // Assert timing is still correct, should be but it's a good reference for the rest of the test.
            Assert.That(sGameTiming.CurTick, Is.EqualTo(new GameTick(18)));
            Assert.That(cGameTiming.CurTick, Is.EqualTo(new GameTick(20)));
            Assert.That(cGameTiming.LastProcessedTick, Is.EqualTo(new GameTick(16)));

            {
                // Send event to server to change flag again, this time to disable it..
                await client.WaitPost(() =>
                {
                    cEntityManager.RaisePredictiveEvent(new SetFooMessage(serverEnt, false));

                    Assert.That(clientComponent.Foo, Is.False);
                });

                // Event correctly arrived on client system.
                Assert.That(clientSystem.EventTriggerList,
                    Is.EquivalentTo(new[] {(new GameTick(20), true, true, false, false)}));
                clientSystem.EventTriggerList.Clear();

                for (var i = 0; i < 2; i++)
                {
                    await server.WaitRunTicks(1);

                    // Event did not arrive on server.
                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(20), false, true, false, false)}));
                    clientSystem.EventTriggerList.Clear();
                }

                {
                    await server.WaitRunTicks(1);

                    // Event arrived on server at tick 20.
                    Assert.That(sGameTiming.CurTick, Is.EqualTo(new GameTick(21)));
                    // But the server didn't listen!
                    Assert.That(serverSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(20), true, true, true, false)}));
                    serverSystem.EventTriggerList.Clear();

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(20), false, true, false, false)}));
                    clientSystem.EventTriggerList.Clear();
                }

                {
                    await server.WaitRunTicks(1);

                    // Nothing happened on server.
                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event no longer got repeated and flag was *not* set by server state.
                    // Mispredict gracefully handled!
                    Assert.That(clientSystem.EventTriggerList, Is.Empty);
                    Assert.That(clientComponent.Foo, Is.True);
                    clientSystem.EventTriggerList.Clear();
                }
            }

            // Re-allow changes to make everything work correctly again.
            serverSystem.Allow = true;

            // Assert timing is still correct.
            Assert.That(sGameTiming.CurTick, Is.EqualTo(new GameTick(22)));
            Assert.That(cGameTiming.CurTick, Is.EqualTo(new GameTick(24)));
            Assert.That(cGameTiming.LastProcessedTick, Is.EqualTo(new GameTick(20)));

            {
                // Send first event to disable the flag (reminder: it never got accepted by the server).
                await client.WaitPost(() =>
                {
                    cEntityManager.RaisePredictiveEvent(new SetFooMessage(serverEnt, false));

                    Assert.That(clientComponent.Foo, Is.False);
                });

                // Event correctly arrived on client system.
                Assert.That(clientSystem.EventTriggerList,
                    Is.EquivalentTo(new[] {(new GameTick(24), true, true, false, false)}));
                clientSystem.EventTriggerList.Clear();

                // Run one tick, everything checks out.
                {
                    await server.WaitRunTicks(1);

                    // Event did not arrive on server.
                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(24), false, true, false, false)}));
                    clientSystem.EventTriggerList.Clear();
                }

                // Send another event, to re-enable it.
                await client.WaitPost(() =>
                {
                    cEntityManager.RaisePredictiveEvent(new SetFooMessage(serverEnt, true));

                    Assert.That(clientComponent.Foo, Is.True);
                });

                // Event correctly arrived on client system.
                Assert.That(clientSystem.EventTriggerList,
                    Is.EquivalentTo(new[] {(new GameTick(25), true, false, true, true)}));
                clientSystem.EventTriggerList.Clear();

                // Next tick we run, both events come in, but at different times.
                {
                    await server.WaitRunTicks(1);

                    // Event did not arrive on server.
                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[]
                        {
                            (new GameTick(24), false, true, false, false), (new GameTick(25), false, false, true, true)
                        }));
                    clientSystem.EventTriggerList.Clear();
                }

                // FIRST event arrives on server!
                {
                    await server.WaitRunTicks(1);

                    Assert.That(serverSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(24), true, true, false, false)}));
                    serverSystem.EventTriggerList.Clear();

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[]
                        {
                            (new GameTick(24), false, true, false, false), (new GameTick(25), false, false, true, true)
                        }));
                    clientSystem.EventTriggerList.Clear();
                }

                // SECOND event arrived on server, client receives ack for first event,
                // still runs second event as past prediction.
                {
                    await server.WaitRunTicks(1);

                    Assert.That(serverSystem.EventTriggerList,
                        Is.EquivalentTo(new[] {(new GameTick(25), true, false, true, true)}));
                    serverSystem.EventTriggerList.Clear();

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList,
                        Is.EquivalentTo(new[]
                        {
                            (new GameTick(25), false, false, true, true)
                        }));
                    clientSystem.EventTriggerList.Clear();
                }

                // Finally, second event acknowledged on client and we're good.
                {
                    await server.WaitRunTicks(1);

                    Assert.That(serverSystem.EventTriggerList, Is.Empty);

                    await client.WaitRunTicks(1);

                    // Event got repeated on client as a past prediction.
                    Assert.That(clientSystem.EventTriggerList, Is.Empty);

                    Assert.That(clientComponent.Foo, Is.True);
                }
            }
            
            cfg.SetCVar(CVars.NetLogging, log);
            await pairTracker.CleanReturnAsync();
        }

        [NetworkedComponent()]
        public sealed class PredictionTestComponent : Component
        {
            private bool _foo;

            public bool Foo
            {
                get => _foo;
                set
                {
                    _foo = value;
                    Dirty();
                }
            }

            public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
            {
                if (curState is not PredictionComponentState pred)
                {
                    return;
                }

                Foo = pred.Foo;
            }

            public override ComponentState GetComponentState()
            {
                return new PredictionComponentState(Foo);
            }

            [Serializable, NetSerializable]
            private sealed class PredictionComponentState : ComponentState
            {
                public bool Foo { get; }

                public PredictionComponentState(bool foo)
                {
                    Foo = foo;
                }
            }
        }

        [Reflect(false)]
        public sealed class PredictionTestEntitySystem : EntitySystem
        {
            public bool Allow { get; set; } = true;

            // Queue of all the events that come in so we can test that they come in perfectly as expected.
            public List<(GameTick tick, bool firstPredict, bool old, bool @new, bool value)> EventTriggerList { get; } =
                new();

            [Dependency] private readonly IGameTiming _gameTiming = default!;

            public override void Initialize()
            {
                base.Initialize();

                SubscribeNetworkEvent<SetFooMessage>(HandleMessage);
                SubscribeLocalEvent<SetFooMessage>(HandleMessage);
            }

            private void HandleMessage(SetFooMessage message, EntitySessionEventArgs args)
            {
                var component = IoCManager.Resolve<IEntityManager>().GetComponent<PredictionTestComponent>(message.Uid);
                var old = component.Foo;
                if (Allow)
                {
                    component.Foo = message.NewFoo;
                }

                EventTriggerList.Add((_gameTiming.CurTick, _gameTiming.IsFirstTimePredicted, old, component.Foo,
                    message.NewFoo));
            }
        }

        private sealed class SetFooMessage : EntityEventArgs
        {
            public SetFooMessage(EntityUid uid, bool newFoo)
            {
                Uid = uid;
                NewFoo = newFoo;
            }

            public EntityUid Uid { get; }
            public bool NewFoo { get; }
        }
    }
}
