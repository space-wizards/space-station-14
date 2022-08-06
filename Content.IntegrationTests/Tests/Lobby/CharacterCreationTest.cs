using System;
using System.Threading.Tasks;
using Content.Client.Lobby;
using Content.Client.Preferences;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.Lobby
{
    [TestFixture]
    [TestOf(typeof(ClientPreferencesManager))]
    [TestOf(typeof(ServerPreferencesManager))]
    public sealed class CharacterCreationTest
    {
        [Test]
        public async Task CreateDeleteCreateTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{InLobby = true});
            var server = pairTracker.Pair.Server;
            var client = pairTracker.Pair.Client;

            var clientNetManager = client.ResolveDependency<IClientNetManager>();
            var clientStateManager = client.ResolveDependency<IStateManager>();
            var clientPrefManager = client.ResolveDependency<IClientPreferencesManager>();

            var serverPrefManager = server.ResolveDependency<IServerPreferencesManager>();


            // Need to run them in sync to receive the messages.
            await PoolManager.RunTicksSync(pairTracker.Pair, 1);

            await PoolManager.WaitUntil(client, () => clientStateManager.CurrentState is LobbyState, 600);

            Assert.NotNull(clientNetManager.ServerChannel);

            var clientNetId = clientNetManager.ServerChannel.UserId;
            HumanoidCharacterProfile profile = null;

            await client.WaitAssertion(() =>
            {
                clientPrefManager.SelectCharacter(0);

                var clientCharacters = clientPrefManager.Preferences?.Characters;
                Assert.That(clientCharacters, Is.Not.Null);
                Assert.That(clientCharacters.Count, Is.EqualTo(1));

                Assert.That(clientStateManager.CurrentState, Is.TypeOf<LobbyState>());

                profile = HumanoidCharacterProfile.Random();
                clientPrefManager.CreateCharacter(profile);

                clientCharacters = clientPrefManager.Preferences?.Characters;

                Assert.That(clientCharacters, Is.Not.Null);
                Assert.That(clientCharacters.Count, Is.EqualTo(2));
                Assert.That(clientCharacters[1].MemberwiseEquals(profile));
            });

            await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(clientNetId).Characters.Count == 2, maxTicks: 60);

            await server.WaitAssertion(() =>
            {
                var serverCharacters = serverPrefManager.GetPreferences(clientNetId).Characters;

                Assert.That(serverCharacters.Count, Is.EqualTo(2));
                Assert.That(serverCharacters[1].MemberwiseEquals(profile));
            });

            await client.WaitAssertion(() =>
            {
                clientPrefManager.DeleteCharacter(1);

                var clientCharacters = clientPrefManager.Preferences?.Characters.Count;
                Assert.That(clientCharacters, Is.EqualTo(1));
            });

            await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(clientNetId).Characters.Count == 1, maxTicks: 60);

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

                var clientCharacters = clientPrefManager.Preferences?.Characters;

                Assert.That(clientCharacters, Is.Not.Null);
                Assert.That(clientCharacters.Count, Is.EqualTo(2));
                Assert.That(clientCharacters[1].MemberwiseEquals(profile));
            });

            await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(clientNetId).Characters.Count == 2, maxTicks: 60);

            await server.WaitAssertion(() =>
            {
                var serverCharacters = serverPrefManager.GetPreferences(clientNetId).Characters;

                Assert.That(serverCharacters.Count, Is.EqualTo(2));
                Assert.That(serverCharacters[1].MemberwiseEquals(profile));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
