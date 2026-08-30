#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Round;

[TestFixture]
public sealed class JobTest : GameTest
{
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    private static readonly ProtoId<JobPrototype> Engineer = "StationEngineer";
    private static readonly ProtoId<JobPrototype> Captain = "Captain";

    private static string _map = "JobTestMap";
    private const string JobWeightOverrideMap = "JobWeightOverrideTestMap";
    private const string JobWeightOverride = "JobWeightOverride";

    [TestPrototypes]
    private static readonly string JobTestMap = @$"
- type: gameMap
  id: {_map}
  mapName: {_map}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
        - type: StationJobs
          availableJobs:
            {Passenger}: [ -1, -1 ]
            {Engineer}: [ -1, -1 ]
            {Captain}: [ 1, 1 ]

- type: gameMap
  id: {JobWeightOverrideMap}
  mapName: {JobWeightOverrideMap}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  jobWeights: {JobWeightOverride}
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
        - type: StationJobs
          availableJobs:
            {Passenger}: [ 1, 1 ]
            {Engineer}: [ 1, 1 ]
            {Captain}: [ 1, 1 ]

- type: jobWeight
  id: {JobWeightOverride}
  weights:
    {Passenger}: 30
";

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    private void AssertJob(TestPair pair, ProtoId<JobPrototype> job, NetUserId? user = null, bool isAntag = false)
    {
        var jobSys = pair.Server.System<SharedJobSystem>();
        var mindSys = pair.Server.System<MindSystem>();
        var roleSys = pair.Server.System<RoleSystem>();
        var ticker = pair.Server.System<GameTicker>();

        user ??= pair.Client.User!.Value;

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses[user.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));

        var uid = pair.Server.PlayerMan.SessionsDict.GetValueOrDefault(user.Value)?.AttachedEntity;
        Assert.That(pair.Server.EntMan.EntityExists(uid));
        var mind = mindSys.GetMind(uid!.Value);
        Assert.That(pair.Server.EntMan.EntityExists(mind));
        Assert.That(jobSys.MindTryGetJobId(mind, out var actualJob));
        Assert.That(actualJob, Is.EqualTo(job));
        Assert.That(roleSys.MindIsAntagonist(mind), Is.EqualTo(isAntag));
    }

    /// <summary>
    /// Simple test that checks that starting the round spawns the player into the test map as a passenger.
    /// </summary>
    [Test]
    public async Task StartRoundTest()
    {
        var pair = Pair;

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();

        // Initially in the lobby
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        // Ready up and start the round
        ticker.ToggleReadyAll(true);
        Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Passenger);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// Check that job preferences are respected.
    /// </summary>
    [Test]
    public async Task JobPreferenceTest()
    {
        var pair = Pair;

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        await pair.SetJobPriorities((Passenger, JobPriority.Never), (Engineer, JobPriority.High));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Engineer);

        await pair.Server.WaitPost(() => ticker.RestartRound());
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        await pair.SetJobPriorities((Passenger, JobPriority.High), (Engineer, JobPriority.Never));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Passenger);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// Check high priority jobs (e.g., captain) are selected before other roles, even if it means a player does not
    /// get their preferred job.
    /// </summary>
    [Test]
    public async Task JobWeightTest()
    {
        var pair = Pair;

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        var stationJobs = pair.Server.System<StationJobsSystem>();
        var captain = pair.Server.ProtoMan.Index(Captain);
        var engineer = pair.Server.ProtoMan.Index(Engineer);
        var passenger = pair.Server.ProtoMan.Index(Passenger);
        Assert.That(stationJobs.TryGetJobWeight(captain, null, out var captainWeight), Is.True);
        Assert.That(stationJobs.TryGetJobWeight(engineer, null, out var engineerWeight), Is.True);
        Assert.That(stationJobs.TryGetJobWeight(passenger, null, out var passengerWeight), Is.True);
        Assert.That(captainWeight, Is.GreaterThan(engineerWeight));
        Assert.That(engineerWeight, Is.EqualTo(passengerWeight));

        await pair.SetJobPriorities((Passenger, JobPriority.Medium), (Engineer, JobPriority.High), (Captain, JobPriority.Low));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Captain);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// Check that map job-weight overrides are used, while jobs omitted by the map retain their default weight.
    /// </summary>
    [Test]
    public async Task MapJobWeightOverrideTest()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, JobWeightOverrideMap);
        var ticker = pair.Server.System<GameTicker>();

        var stationJobs = pair.Server.System<StationJobsSystem>();
        var passenger = pair.Server.ProtoMan.Index(Passenger);
        var engineer = pair.Server.ProtoMan.Index(Engineer);
        var captain = pair.Server.ProtoMan.Index(Captain);
        var map = pair.Server.ProtoMan.Index<GameMapPrototype>(JobWeightOverrideMap);
        Assert.That(stationJobs.TryGetJobWeight(passenger, map.JobWeights, out var passengerWeight), Is.True);
        Assert.That(stationJobs.TryGetJobWeight(engineer, map.JobWeights, out var engineerWeight), Is.True);
        Assert.That(stationJobs.TryGetJobWeight(captain, map.JobWeights, out var captainWeight), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(passengerWeight, Is.EqualTo(30));
            Assert.That(engineerWeight, Is.LessThan(captainWeight));
            Assert.That(engineerWeight, Is.EqualTo(0));
        });
        Assert.That(JobUIComparer.TryCreate(pair.Server.ProtoMan, map.JobWeights, out var comparer), Is.True);
        Assert.That(comparer!.Compare(passenger, captain), Is.LessThan(0));

        await pair.Server.AddDummySessions(2);
        await pair.RunTicksSync(5);

        var players = pair.Server.PlayerMan.Sessions.Select(x => x.UserId).ToArray();
        Assert.That(players, Has.Length.EqualTo(3));

        await pair.SetJobPriorities(players[0], (Passenger, JobPriority.Medium), (Captain, JobPriority.High));
        await pair.SetJobPriorities(players[1], (Passenger, JobPriority.Never), (Engineer, JobPriority.High), (Captain, JobPriority.Medium));
        await pair.SetJobPriorities(players[2], (Passenger, JobPriority.Never), (Engineer, JobPriority.High));

        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        var stationData = pair.Server.EntMan.EntityQuery<StationDataComponent>().Single();
        Assert.That(stationData.JobWeights, Is.EqualTo(map.JobWeights));

        // Passenger's map weight of 30 takes precedence over the captain's default weight of 20,
        // even though this player prefers captain. Engineer has no map override and keeps its default weight.
        AssertJob(pair, Passenger, players[0]);
        AssertJob(pair, Captain, players[1]);
        AssertJob(pair, Engineer, players[2]);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// Check that jobs are preferentially given to players that have marked those jobs as higher priority.
    /// </summary>
    [Test]
    public async Task JobPriorityTest()
    {
        var pair = Pair;

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        await pair.Server.AddDummySessions(5);
        await pair.RunTicksSync(5);

        var engineers = pair.Server.PlayerMan.Sessions.Select(x => x.UserId).ToList();
        var captain = engineers[3];
        engineers.RemoveAt(3);

        await pair.SetJobPriorities(captain, (Passenger, JobPriority.Never), (Captain, JobPriority.High), (Engineer, JobPriority.Medium));
        foreach (var engi in engineers)
        {
            await pair.SetJobPriorities(engi, (Passenger, JobPriority.Never), (Captain, JobPriority.Medium), (Engineer, JobPriority.High));
        }

        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Captain, captain);
        Assert.Multiple(() =>
        {
            foreach (var engi in engineers)
            {
                AssertJob(pair, Engineer, engi);
            }
        });

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }
}
