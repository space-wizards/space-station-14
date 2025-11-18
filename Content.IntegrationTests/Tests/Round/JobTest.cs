#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Round;

[TestFixture]
public sealed class JobTest
{
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    private static readonly ProtoId<JobPrototype> Engineer = "StationEngineer";
    private static readonly ProtoId<JobPrototype> Captain = "Captain";

    private static string _map = "JobTestMap";

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
";

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
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

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
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Check that job preferences are respected.
    /// </summary>
    [Test]
    public async Task JobPreferenceTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        await pair.SetJobPriorities((Passenger, JobPriority.Medium), (Engineer, JobPriority.High));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Engineer);

        await pair.Server.WaitPost(() => ticker.RestartRound());
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        await pair.SetJobPriorities((Passenger, JobPriority.High), (Engineer, JobPriority.Medium));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Passenger);

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Check high priority jobs (e.g., captain) are selected before other roles, even if it means a player does not
    /// get their preferred job.
    /// </summary>
    [Test]
    public async Task JobWeightTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        var captain = pair.Server.ProtoMan.Index(Captain);
        var engineer = pair.Server.ProtoMan.Index(Engineer);
        var passenger = pair.Server.ProtoMan.Index(Passenger);
        Assert.That(captain.Weight, Is.GreaterThan(engineer.Weight));
        Assert.That(engineer.Weight, Is.EqualTo(passenger.Weight));

        await pair.SetJobPriorities((Passenger, JobPriority.Medium), (Engineer, JobPriority.High), (Captain, JobPriority.Low));
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);

        AssertJob(pair, Captain);

        await pair.Server.WaitPost(() => ticker.RestartRound());
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Check that jobs are preferentially given to players that have marked those jobs as higher priority.
    /// </summary>
    [Test]
    public async Task JobPriorityTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        pair.Server.CfgMan.SetCVar(CCVars.GameMap, _map);
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(pair.Client.AttachedEntity, Is.Null);

        await pair.Server.AddDummySessions(5);
        await pair.RunTicksSync(5);

        var engineers = pair.Server.PlayerMan.Sessions.Select(x => x.UserId).ToList();
        var captain = engineers[3];
        engineers.RemoveAt(3);

        await pair.SetJobPriorities(captain, (Captain, JobPriority.High), (Engineer, JobPriority.Medium));
        foreach (var engi in engineers)
        {
            await pair.SetJobPriorities(engi, (Captain, JobPriority.Medium), (Engineer, JobPriority.High));
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
        await pair.CleanReturnAsync();
    }
}
