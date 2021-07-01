using System.Linq;
using System.Threading.Tasks;
using Content.Server.Construction.Completions;
using Content.Server.Construction.Components;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public class ComputerIntegration : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await RunTicksSync(client, server, 10);

            IPlayerSession serverPlayer = null;
            IEntity serverPlayerMob = null;
            IEntity serverScrewdriver = null;
            IEntity serverComputer = null;
            var computerCoords = EntityCoordinates.Invalid;

            var entityMan = server.ResolveDependency<IEntityManager>();
            var playerMan = server.ResolveDependency<IPlayerManager>();
            var entityLookup = server.ResolveDependency<IEntityLookup>();
            var interaction = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            void FindComputer()
            {
                serverComputer = entityLookup.GetEntitiesAt(computerCoords.GetMapId(entityMan),
                    computerCoords.ToMapPos(entityMan)).First();
            }

            server.Post(() =>
            {
                serverPlayer = playerMan.GetAllPlayers()[0];
                serverPlayerMob = serverPlayer.AttachedEntity;

                Logger.Info("We spawn the screwdriver...");
                serverScrewdriver = entityMan.SpawnEntity("Screwdriver", serverPlayerMob!.Transform.Coordinates);
                var hands = serverPlayerMob.GetComponent<HandsComponent>();
                hands.ActiveHand = hands.HandNames.First();

                Assert.That(hands.TryPickupEntity(hands.ActiveHand, serverScrewdriver, false), Is.True);
                Logger.Info("And the mob has picked it up.");

                Logger.Info("We spawn the computer...");
                computerCoords = serverPlayerMob.Transform.Coordinates.Offset(-Vector2.One);
                serverComputer = entityMan.SpawnEntity("ComputerId", computerCoords);
            });

            await RunTicksSync(client, server, 10);

            for (int i = 0; i < 5; i++)
            {
                server.Post(() =>
                {
                    Logger.Info($"-- LOOP {i} --");

                    Logger.Info("Now the mob will unscrew the computer...");
                    interaction.UserInteraction(serverPlayerMob, computerCoords, serverComputer.Uid);
                    Assert.That(serverComputer.Deleted);
                    FindComputer();

                    Assert.That(serverComputer.Prototype?.ID ?? "", Is.EqualTo("ComputerFrame"));
                });

                await RunTicksSync(client, server, 1);

                server.Post(() =>
                {
                    Logger.Info("Now the mob will screw the computer...");
                    interaction.UserInteraction(serverPlayerMob, computerCoords, serverComputer.Uid);
                    Assert.That(serverComputer.Deleted);
                    FindComputer();

                    Assert.That(serverComputer.Prototype?.ID ?? "", Is.EqualTo("ComputerId"));
                });

                await RunTicksSync(client, server, 1);
            }
        }
    }
}
