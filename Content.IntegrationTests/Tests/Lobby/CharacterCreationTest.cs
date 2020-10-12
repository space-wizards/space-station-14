using System.Threading.Tasks;
using Content.Client;
using Content.Client.Interfaces;
using Content.Client.State;
using Content.Server.GameTicking;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Preferences;
using Content.Shared;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Client.Interfaces.State;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;

namespace Content.IntegrationTests.Tests.Lobby
{
    [TestFixture]
    [TestOf(typeof(ClientPreferencesManager))]
    [TestOf(typeof(ServerPreferencesManager))]
    public class CharacterCreationTest : ContentIntegrationTest
    {
        [Test]
        public async Task CreateDeleteCreateTest()
        {
            var (client, server) = await StartConnectedServerClientPair();

            var clientNetManager = client.ResolveDependency<IClientNetManager>();
            var clientStateManager = client.ResolveDependency<IStateManager>();
            var clientPrefManager = client.ResolveDependency<IClientPreferencesManager>();

            var serverConfig = server.ResolveDependency<IConfigurationManager>();
            var serverTicker = server.ResolveDependency<IGameTicker>();
            var serverPrefManager = server.ResolveDependency<IServerPreferencesManager>();

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                var lobbyCvar = CCVars.GameLobbyEnabled;
                serverConfig.SetCVar(lobbyCvar.Name, true);

                serverTicker.RestartRound();
            });

            Assert.That(serverTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

            await WaitUntil(client, () => clientStateManager.CurrentState is LobbyState, maxTicks: 60);

            Assert.NotNull(clientNetManager.ServerChannel);

            var clientNetId = clientNetManager.ServerChannel.UserId;
            HumanoidCharacterProfile profile = null;

            await client.WaitAssertion(() =>
            {
                clientPrefManager.SelectCharacter(0);

                var clientCharacters = clientPrefManager.Preferences.Characters;
                Assert.That(clientCharacters.Count, Is.EqualTo(1));

                Assert.That(clientStateManager.CurrentState, Is.TypeOf<LobbyState>());

                profile = HumanoidCharacterProfile.Random();
                clientPrefManager.CreateCharacter(profile);

                clientCharacters = clientPrefManager.Preferences.Characters;

                Assert.That(clientCharacters.Count, Is.EqualTo(2));
                Assert.That(clientCharacters[1].MemberwiseEquals(profile));
            });

            await WaitUntil(server, () => serverPrefManager.GetPreferences(clientNetId).Characters.Count == 2, maxTicks: 60);

            await server.WaitAssertion(() =>
            {
                var serverCharacters = serverPrefManager.GetPreferences(clientNetId).Characters;

                Assert.That(serverCharacters.Count, Is.EqualTo(2));
                Assert.That(serverCharacters[1].MemberwiseEquals(profile));
            });

            await client.WaitAssertion(() =>
            {
                clientPrefManager.DeleteCharacter(1);

                var clientCharacters = clientPrefManager.Preferences.Characters.Count;
                Assert.That(clientCharacters, Is.EqualTo(1));
            });

            await WaitUntil(server, () => serverPrefManager.GetPreferences(clientNetId).Characters.Count == 1, maxTicks: 60);

            await server.WaitAssertion(() =>
            {
                var serverCharacters = serverPrefManager.GetPreferences(clientNetId).Characters.Count;
                Assert.That(serverCharacters, Is.EqualTo(1));
            });

            await client.WaitIdleAsync();

            await client.WaitAssertion(() =>
            {
                profile = HumanoidCharacterProfile.Random();

                clientPrefManager.CreateCharacter(profile);

                var clientCharacters = clientPrefManager.Preferences.Characters;

                Assert.That(clientCharacters.Count, Is.EqualTo(2));
                Assert.That(clientCharacters[1].MemberwiseEquals(profile));
            });

            await WaitUntil(server, () => serverPrefManager.GetPreferences(clientNetId).Characters.Count == 2, maxTicks: 60);

            await server.WaitAssertion(() =>
            {
                var serverCharacters = serverPrefManager.GetPreferences(clientNetId).Characters;

                Assert.That(serverCharacters.Count, Is.EqualTo(2));
                Assert.That(serverCharacters[1].MemberwiseEquals(profile));
            });
        }
    }
}
