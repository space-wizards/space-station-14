﻿using System.Collections.Generic;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
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
            cPref.UpdateCharacter(humanoidZero.WithAge(age).WithJobPreferences(priorities.Keys), 0);
            cPref.UpdateJobPriorities(priorities);
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
    /// This tests that age requirements are actually used for the final profile selection.
    /// This makes and enables 29 profiles that fail the age requirement but request a job.
    /// 1 profile is made that will pass the age requirement.
    /// It asserts that after the game is started, the single profile that meets the age requirement is properly selected.
    /// </summary>
    [Test]
    [TestCase]
    public async Task AgeRequirementsTestMultipleCharacters()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);

        var ticker = pair.Server.System<GameTicker>();

        var cPref = pair.Client.ResolveDependency<IClientPreferencesManager>();

        await pair.ReallyBeIdle();

        var humanoidZero = cPref.Preferences!.Characters[0] as HumanoidCharacterProfile;
        Assert.That(humanoidZero, Is.Not.Null);

        var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
        {
            { "SeniorCitizen", JobPriority.High },
            { "Passenger", JobPriority.Low },
        };

        await pair.Client.WaitAssertion(() =>
        {
            humanoidZero = humanoidZero.WithJobPreferences(priorities.Keys);
            cPref.UpdateCharacter(humanoidZero.WithAge(75), 0);
            var maxSlots = cPref.Settings?.MaxCharacterSlots ?? 30;
            for (var i = 1; i < maxSlots; i++)
            {
                cPref.UpdateCharacter(humanoidZero.WithAge(20), i);
            }
            cPref.UpdateJobPriorities(priorities);
        });

        await pair.ReallyBeIdle();

        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        pair.AssertJob("SeniorCitizen");
        Assert.That(pair.Client.AttachedEntity, Is.Not.Null);
        pair.Server.EntMan.TryGetComponent<HumanoidAppearanceComponent>(pair.ToServerUid(pair.Client.AttachedEntity.Value), out var appearance);
        Assert.That(appearance, Is.Not.Null);
        Assert.That(appearance.Age, Is.EqualTo(75));

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
            cPref.UpdateCharacter(humanoidZero.WithSpecies(species).WithJobPreferences(priorities.Keys), 0);
            cPref.UpdateJobPriorities(priorities);
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
}
