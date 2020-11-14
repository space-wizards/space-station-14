using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Suspicion;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SSS
{
    [TestFixture]
    [TestOf(typeof(SuspicionRoleComponent))]
    public class TraitorAllyTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestTraitorKnowsAllies()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var clientAmount = 3;
            var clients = new ClientIntegrationInstance[clientAmount];

            for (var i = 0; i < clients.Length; i++)
            {
                clients[i] = StartClient();
            }

            await Task.WhenAll(clients.Select(client => client.WaitIdleAsync()));

            foreach (var client in clients)
            {
                client.SetConnectTarget(server);

                await client.WaitPost(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));
            }

            for (var i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await Task.WhenAll(clients.Select(client => client.WaitRunTicks(1)));
            }

            await server.WaitIdleAsync();

            var humans = new (IEntity entity, SuspicionRoleComponent suspicion, MindComponent mind)[clientAmount];

            var sMapManager = server.ResolveDependency<IMapManager>();
            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            await server.WaitPost(() =>
            {
                sMapManager.CreateMap();
            });

            await Task.WhenAll(clients.Select(client => client.WaitIdleAsync()));

            for (var i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await Task.WhenAll(clients.Select(client => client.WaitRunTicks(1)));
            }

            await server.WaitPost(() =>
            {
                foreach (var player in sPlayerManager.GetAllPlayers())
                {
                    player.JoinGame();
                }
            });

            for (var i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await Task.WhenAll(clients.Select(client => client.WaitRunTicks(1)));
            }

            await server.WaitAssertion(() =>
            {
                var players = sPlayerManager.GetAllPlayers();

                Assert.That(players.Count, Is.EqualTo(clientAmount));

                for (var i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    var entity = player.AttachedEntity;

                    Assert.NotNull(entity);

                    var suspicion = entity.EnsureComponent<SuspicionRoleComponent>();
                    var mind = entity.EnsureComponent<MindComponent>();

                    humans[i] = (entity, suspicion, mind);
                }

                for (var i = 0; i < 1; i++)
                {
                    var human = humans[i];
                    var mindComponent = human.mind;

                    Assert.True(mindComponent.HasMind);
                    Assert.NotNull(mindComponent.Mind);

                    var antagPrototype = sPrototypeManager.Index<AntagPrototype>(PresetSuspicion.TraitorID);
                    var role = new SuspicionTraitorRole(mindComponent.Mind, antagPrototype);

                    mindComponent.Mind.AddRole(role);

                    var suspicion = human.suspicion;

                    Assert.That(suspicion.Role, Is.EqualTo(role));
                    Assert.That(suspicion.IsTraitor);
                    Assert.That(suspicion.KnowsAllies);
                    Assert.False(suspicion.IsInnocent());
                    Assert.False(suspicion.IsDead());
                }

                var firstTraitor = humans[0];
                var secondTraitor = humans[1];

                Assert.That(firstTraitor.suspicion.KnowsAllies);
                Assert.That(secondTraitor.suspicion.KnowsAllies);

                // Both traitors know their ally
                Assert.That(firstTraitor.suspicion.Allies, Does.Contain(secondTraitor.suspicion));
                Assert.That(secondTraitor.suspicion.Allies, Does.Contain(firstTraitor.suspicion));

                // They do not count as an ally for themselves
                Assert.That(firstTraitor.suspicion.Allies, Does.Not.Contain(firstTraitor.suspicion));
                Assert.That(secondTraitor.suspicion.Allies, Does.Not.Contain(secondTraitor.suspicion));
            });
        }
    }
}
