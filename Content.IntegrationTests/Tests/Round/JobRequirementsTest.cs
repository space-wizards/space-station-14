using System.Collections.Generic;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Round;

[TestFixture]
[TestOf(typeof(JobRequirementsManager))]
public sealed class JobRequirementsTest
{
    private static string _map = "JobRequirementsTestMap";

    [TestPrototypes]
    private const string Prototypes = """
        - type: playTimeTracker
          id: PlayTimeDummySeniorCitizen

        - type: playTimeTracker
          id: PlayTimeDummyTwenties

        - type: job
          id: SeniorCitizen
          playTimeTracker: PlayTimeDummySeniorCitizen
          requirements:
          - !type:AgeRequirement
            requiredAge: 65

        - type: roleLoadout
          id: JobSeniorCitizen

        - type: job
          id: Twenties
          playTimeTracker: PlayTimeDummyTwenties
          requirements:
          - !type:AgeRequirement
            requiredAge: 20
          - !type:AgeRequirement
            requiredAge: 29
            inverted: true

        - type: roleLoadout
          id: JobTwenties

        - type: playTimeTracker
          id: PlayTimeDummyWehngineer

        - type: playTimeTracker
          id: PlayTimeDummyFreezerHead

        - type: job
          id: Wehngineer
          playTimeTracker: PlayTimeDummyWehngineer
          requirements:
          - !type:SpeciesRequirement
            species:
            - Reptilian

        - type: roleLoadout
          id: JobWehngineer

        - type: job
          id: FreezerHead
          playTimeTracker: PlayTimeDummyFreezerHead
          requirements:
          - !type:SpeciesRequirement
            inverted: true
            species:
            - Reptilian

        - type: roleLoadout
          id: JobFreezerHead

        - type: playTimeTracker
          id: PlayTimeDummyRadioAnnouncer

        - type: job
          id: RadioAnnouncer
          playTimeTracker: PlayTimeDummyRadioAnnouncer
          requirements:
          - !type:TraitsRequirement
            inverted: true
            traits:
            - Muted

        - type: roleLoadout
          id: JobRadioAnnouncer

        - type: playTimeTracker
          id: PlayTimeDummyDaredevil

        - type: job
          id: Daredevil
          playTimeTracker: PlayTimeDummyDaredevil
          requirements:
          - !type:TraitsRequirement
            traits:
            - Unrevivable

        - type: roleLoadout
          id: JobDaredevil

        - type: gameMap
          id: JobRequirementsTestMap
          mapName: JobRequirementsTestMap
          mapPath: /Maps/Test/empty.yml
          minPlayers: 0
          stations:
            Empty:
              mapNameTemplate: JobRequirementsTestMap
              stationProto: StandardNanotrasenStation
              components:
                - type: StationJobs
                  availableJobs:
                    SeniorCitizen: [ -1, -1 ]
                    Passenger: [ -1, -1 ]
                    Twenties: [ -1, -1 ]
                    Wehngineer: [ -1, -1 ]
                    FreezerHead: [ -1, -1 ]
                    RadioAnnouncer: [ -1, -1 ]
                    Daredevil: [ -1, -1 ]
        """;

    /// <summary>
    /// Generic test for the age requirements
    /// </summary>
    /// <param name="age">Age of created character</param>
    /// <param name="wantedJob">Job that this character wants</param>
    /// <param name="expectedJob">If true, assert that the job was assigned.
    /// If false, assert that job was not given</param>
    [Test]
    [TestCase(75, "SeniorCitizen")]
    [TestCase(20, "SeniorCitizen", false)]
    [TestCase(19, "Twenties", false)]
    [TestCase(20, "Twenties")]
    [TestCase(29, "Twenties")]
    [TestCase(30, "Twenties", false)]
    public async Task AgeRequirementsTest(int age, string wantedJob, bool expectedJob = true)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameRoleTimers, false);   // should not need timers to test age
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);

        var ticker = pair.Server.System<GameTicker>();

        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();

        await pair.ReallyBeIdle();

        var humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);

        var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
        {
            { wantedJob, JobPriority.High },
            { "Passenger", JobPriority.Low },
        };

        await pair.Client.WaitAssertion(() =>
        {
            cPref.UpdateCharacter(humanoidZero.WithAge(age).WithJobPriorities(priorities), 0);
        });

        await pair.ReallyBeIdle();

        humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);
        Assert.That(humanoidZero.Age, Is.EqualTo(age));

        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        pair.AssertJob(expectedJob ? wantedJob : "Passenger");

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Generic test for species requirements
    /// </summary>
    /// <param name="species">Species of the created profile</param>
    /// <param name="wantedJob">Job preference of the created profile</param>
    /// <param name="expectedJob">If true, assert that the job was assigned.
    /// If false, assert that job was not given</param>
    [Test]
    [TestCase("Reptilian", "Wehngineer")]
    [TestCase("Moth", "Wehngineer", false)]
    [TestCase("Reptilian", "FreezerHead", false)]
    [TestCase("Moth", "FreezerHead")]
    public async Task SpeciesRequirementsTest(string species, string wantedJob, bool expectedJob = true)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameRoleTimers, false);   // should not need timers to test species requirement
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();

        await pair.ReallyBeIdle();

        var humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);

        var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
        {
            { wantedJob, JobPriority.High },
            { "Passenger", JobPriority.Low },
        };

        await pair.Client.WaitAssertion(() =>
        {
            cPref.UpdateCharacter(humanoidZero.WithSpecies(species).WithJobPriorities(priorities), 0);
        });

        await pair.ReallyBeIdle();

        humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);
        Assert.That(humanoidZero.Species.Id, Is.EqualTo(species));

        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        pair.AssertJob(expectedJob ? wantedJob : "Passenger");

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Generic test for traits requirements
    /// </summary>
    /// <param name="trait1">A trait of the created profile; null if no traits</param>
    /// <param name="trait2">A trait of the created profile; null if zero or one traits</param>
    /// <param name="wantedJob">Job preference of the created profile</param>
    /// <param name="expectedJob">If true, assert that the job was assigned.
    /// If false, assert that job was not given</param>
    [Test]
    [TestCase("Unrevivable", "PirateAccent", "RadioAnnouncer")]
    [TestCase("Muted", "Unrevivable", "RadioAnnouncer", false)]
    [TestCase("Muted", "PirateAccent", "Daredevil", false)]
    [TestCase("Muted", "Unrevivable", "Daredevil")]
    public async Task TraitsRequirementsTest(string? trait1, string? trait2, string wantedJob, bool expectedJob = true)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameRoleTimers, false);   // should not need timers to test species requirement
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();
        var protoMan = pair.Server.ResolveDependency<IPrototypeManager>();

        await pair.ReallyBeIdle();

        var humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);

        var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
        {
            { wantedJob, JobPriority.High },
            { "Passenger", JobPriority.Low },
        };

        await pair.Client.WaitAssertion(() =>
        {
            if (trait1 != null)
                humanoidZero = humanoidZero.WithTraitPreference(trait1, protoMan);
            if (trait2 != null)
                humanoidZero = humanoidZero.WithTraitPreference(trait2, protoMan);
            cPref.UpdateCharacter(humanoidZero.WithJobPriorities(priorities), 0);
        });

        await pair.ReallyBeIdle();

        humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);
        if (trait1 != null)
            Assert.That(humanoidZero.TraitPreferences.Contains(trait1));
        if (trait2 != null)
            Assert.That(humanoidZero.TraitPreferences.Contains(trait2));

        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        pair.AssertJob(expectedJob ? wantedJob : "Passenger");

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }
}
