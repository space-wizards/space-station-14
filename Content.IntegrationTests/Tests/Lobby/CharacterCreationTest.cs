#nullable enable
using Content.Client.Lobby;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Client.State;

namespace Content.IntegrationTests.Tests.Lobby;

[TestOf(typeof(ClientPreferencesManager))]
[TestOf(typeof(ServerPreferencesManager))]
public sealed class CharacterCreationTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        InLobby = true,
    };

    [SidedDependency(Side.Client)] private IClientPreferencesManager _cPrefManager = null!;
    [SidedDependency(Side.Client)] private IStateManager _cStateManager = null!;
    [SidedDependency(Side.Server)] private IServerPreferencesManager _sPrefManager = null!;

    [Test]
    public async Task CreateDeleteCreateTest()
    {
        Assert.That(Client.User, Is.Not.Null);
        var user = Client.User.Value;

        Assert.That(_cStateManager.CurrentState, Is.TypeOf<LobbyState>());
        await Client.WaitPost(() => _cPrefManager.SelectCharacter(0));

        await RunTicksSync(5);

        var clientCharacters = _cPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(1));

        HumanoidCharacterProfile profile = null!;
        await Client.WaitPost(() =>
        {
            profile = HumanoidCharacterProfile.Random();
            _cPrefManager.CreateCharacter(profile);
        });
        await RunTicksSync(5);

        clientCharacters = _cPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        AssertEqual(clientCharacters[1], profile);

        await PoolManager.WaitUntil(Server, () => _sPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);

        var serverCharacters = _sPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        AssertEqual(serverCharacters[1], profile);

        await Client.WaitAssertion(() => _cPrefManager.DeleteCharacter(1));
        await RunTicksSync(5);
        Assert.That(_cPrefManager.Preferences?.Characters, Has.Count.EqualTo(1));
        await PoolManager.WaitUntil(Server, () => _sPrefManager.GetPreferences(user).Characters.Count == 1, maxTicks: 60);
        Assert.That(_sPrefManager.GetPreferences(user).Characters, Has.Count.EqualTo(1));

        await Client.WaitIdleAsync();

        await Client.WaitAssertion(() =>
        {
            profile = HumanoidCharacterProfile.Random();
            _cPrefManager.CreateCharacter(profile);
        });
        await RunTicksSync(5);

        clientCharacters = _cPrefManager.Preferences?.Characters;
        Assert.That(clientCharacters, Is.Not.Null);
        Assert.That(clientCharacters, Has.Count.EqualTo(2));
        AssertEqual(clientCharacters[1], profile);

        await PoolManager.WaitUntil(Server, () => _sPrefManager.GetPreferences(user).Characters.Count == 2, maxTicks: 60);
        serverCharacters = _sPrefManager.GetPreferences(user).Characters;
        Assert.That(serverCharacters, Has.Count.EqualTo(2));
        AssertEqual(serverCharacters[1], profile);
    }

    private static void AssertEqual(HumanoidCharacterProfile a, HumanoidCharacterProfile b)
    {
        if (a.MemberwiseEquals(b))
            return;

        using (Assert.EnterMultipleScope())
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
        }
    }

    private static void AssertEqual(HumanoidCharacterAppearance a, HumanoidCharacterAppearance b)
    {
        if (a.Equals(b))
            return;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.EyeColor, Is.EqualTo(b.EyeColor));
            Assert.That(a.SkinColor, Is.EqualTo(b.SkinColor));
            Assert.That(a.Markings, Is.EquivalentTo(b.Markings));
            Assert.Fail("Appearance not equal");
        }
    }
}
