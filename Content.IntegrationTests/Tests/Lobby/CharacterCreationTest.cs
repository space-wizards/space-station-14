using Content.Client.Lobby;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Client.State;

namespace Content.IntegrationTests.Tests.Lobby;

[TestFixture]
[TestOf(typeof(ClientPreferencesManager))]
[TestOf(typeof(ServerPreferencesManager))]
public sealed class CharacterCreationTest
{
    [Test]
    public async Task CreateDeleteCreateTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        var server = pair.Server;
        var client = pair.Client;
        var user = pair.Client.User!.Value;
        var clientPrefManager = client.Resolve<IClientPreferencesManager>();
        var serverPrefManager = server.Resolve<IServerPreferencesManager>();

        Assert.That(client.Resolve<IStateManager>().CurrentState, Is.TypeOf<LobbyState>());
        await client.WaitPost(() => clientPrefManager.SelectCharacter(0));
        await pair.RunTicksSync(5);

        var clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(1));

        HumanoidCharacterProfile profile = null;
        await client.WaitPost(() =>
        {
            profile = HumanoidCharacterProfile.Random();
            clientPrefManager.CreateCharacter(profile);
        });
        await pair.RunTicksSync(5);

        clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        AssertEqual(clientCharacters[1], profile);

        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);

        var serverCharacters = serverPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        AssertEqual(serverCharacters[1], profile);

        await client.WaitAssertion(() => clientPrefManager.DeleteCharacter(1));
        await pair.RunTicksSync(5);
        Assert.That(clientPrefManager.Preferences?.Characters.Count, Is.EqualTo(1));
        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 1, maxTicks: 60);
        Assert.That(serverPrefManager.GetPreferences(user).Characters.Count, Is.EqualTo(1));

        await client.WaitIdleAsync();

        await client.WaitAssertion(() =>
        {
            profile = HumanoidCharacterProfile.Random();
            clientPrefManager.CreateCharacter(profile);
        });
        await pair.RunTicksSync(5);

        clientCharacters = clientPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        AssertEqual(clientCharacters[1], profile);

        await PoolManager.WaitUntil(server, () => serverPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);
        serverCharacters = serverPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        AssertEqual(serverCharacters[1], profile);
        await pair.CleanReturnAsync();
    }

    private void AssertEqual(ICharacterProfile clientCharacter, HumanoidCharacterProfile b)
    {
        if (clientCharacter.MemberwiseEquals(b))
            return;

        if (clientCharacter is not HumanoidCharacterProfile a)
        {
            Assert.Fail($"Not a {nameof(HumanoidCharacterProfile)}");
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(a.Name, Is.EqualTo(b.Name));
            Assert.That(a.Age, Is.EqualTo(b.Age));
            Assert.That(a.Sex, Is.EqualTo(b.Sex));
            Assert.That(a.Gender, Is.EqualTo(b.Gender));
            Assert.That(a.Species, Is.EqualTo(b.Species));
            Assert.That(a.PreferenceUnavailable, Is.EqualTo(b.PreferenceUnavailable));
            Assert.That(a.SpawnPriority, Is.EqualTo(b.SpawnPriority));
            Assert.That(a.FlavorText, Is.EqualTo(b.FlavorText));
            Assert.That(a.JobPriorities, Is.EquivalentTo(b.JobPriorities));
            Assert.That(a.AntagPreferences, Is.EquivalentTo(b.AntagPreferences));
            Assert.That(a.TraitPreferences, Is.EquivalentTo(b.TraitPreferences));
            Assert.That(a.Loadouts, Is.EquivalentTo(b.Loadouts));
            AssertEqual(a.Appearance, b.Appearance);
            Assert.Fail("Profile not equal");
        });
    }

    private void AssertEqual(HumanoidCharacterAppearance a, HumanoidCharacterAppearance b)
    {
        if (a.MemberwiseEquals(b))
            return;

        Assert.That(a.HairStyleId, Is.EqualTo(b.HairStyleId));
        Assert.That(a.HairColor, Is.EqualTo(b.HairColor));
        Assert.That(a.FacialHairStyleId, Is.EqualTo(b.FacialHairStyleId));
        Assert.That(a.FacialHairColor, Is.EqualTo(b.FacialHairColor));
        Assert.That(a.EyeColor, Is.EqualTo(b.EyeColor));
        Assert.That(a.SkinColor, Is.EqualTo(b.SkinColor));
        Assert.That(a.Markings, Is.EquivalentTo(b.Markings));
        Assert.Fail("Appearance not equal");
    }
}
