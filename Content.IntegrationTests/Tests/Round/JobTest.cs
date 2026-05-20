#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
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

[EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameMap), MapId)]
public sealed class JobTest : GameTest
{
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    private static readonly ProtoId<JobPrototype> Engineer = "StationEngineer";
    private static readonly ProtoId<JobPrototype> Captain = "Captain";

    private const string MapId = "JobTestMap";

    [TestPrototypes]
    private static readonly string JobTestMap = @$"
- type: gameMap
  id: {MapId}
  mapName: {MapId}
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

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    [SidedDependency(Side.Server)] private GameTicker _ticker = null!;
    [SidedDependency(Side.Server)] private SharedJobSystem _sJobSystem = null!;
    [SidedDependency(Side.Server)] private MindSystem _sMindSystem = null!;
    [SidedDependency(Side.Server)] private RoleSystem _sRoleSystem = null!;

    /// <summary>
    /// Simple test that checks that starting the round spawns the player into the test map as a passenger.
    /// </summary>
    [Test]
    [Description("Checks that starting the round spawns the player into the test map as a passenger.")]
    public async Task StartRoundTest()
    {
        // Ready up and start the round
        await ToggleReadyAllAndStartRound();

        AssertJob(Passenger);
    }

    /// <summary>
    /// Check that job preferences are respected.
    /// </summary>
    [Test]
    [Description("Check that job preferences are respected.")]
    public async Task JobPreferenceTest()
    {
        await Pair.SetJobPriorities((Passenger, JobPriority.Medium), (Engineer, JobPriority.High));
        await ToggleReadyAllAndStartRound();

        AssertJob(Engineer);

        await Server.WaitPost(_ticker.RestartRound);
        Assert.That(_ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

        await Pair.SetJobPriorities((Passenger, JobPriority.High), (Engineer, JobPriority.Medium));
        await ToggleReadyAllAndStartRound();

        AssertJob(Passenger);
    }

    /// <summary>
    /// Check high priority jobs (e.g., captain) are selected before other roles, even if it means a player does not
    /// get their preferred job.
    /// </summary>
    [Test]
    [Description("Check high priority jobs are selected before other roles, even if it means a player does not get their preferred job.")]
    public async Task JobWeightTest()
    {
        var captain = SProtoMan.Index(Captain);
        var engineer = SProtoMan.Index(Engineer);
        var passenger = SProtoMan.Index(Passenger);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(captain.Weight, Is.GreaterThan(engineer.Weight));
            Assert.That(engineer.Weight, Is.EqualTo(passenger.Weight));
        }

        await Pair.SetJobPriorities((Passenger, JobPriority.Medium), (Engineer, JobPriority.High), (Captain, JobPriority.Low));
        await ToggleReadyAllAndStartRound();

        AssertJob(Captain);
    }

    /// <summary>
    /// Check that jobs are preferentially given to players that have marked those jobs as higher priority.
    /// </summary>
    [Test]
    public async Task JobPriorityTest()
    {
        await Server.AddDummySessions(5);
        await RunUntilSynced();

        var engineers = Server.PlayerMan.Sessions.Select(x => x.UserId).ToList();
        var captain = engineers[3];
        engineers.RemoveAt(3);

        await Pair.SetJobPriorities(captain, (Captain, JobPriority.High), (Engineer, JobPriority.Medium));
        foreach (var engi in engineers)
        {
            await Pair.SetJobPriorities(engi, (Captain, JobPriority.Medium), (Engineer, JobPriority.High));
        }

        await ToggleReadyAllAndStartRound();

        AssertJob(Captain, captain);
        using (Assert.EnterMultipleScope())
        {
            foreach (var engi in engineers)
            {
                AssertJob(Engineer, engi);
            }
        }
    }

    /// <summary>
    /// A helper to verify that the game is currently in the lobby and the player's status is not ready.
    /// </summary>
    private async Task AssertInLobbyNotReady()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(Client.AttachedEntity, Is.Null);
            Assert.That(_ticker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        }
    }

    /// <summary>
    /// Sets all players' statuses to ready and starts the round.
    /// </summary>
    private async Task ToggleReadyAllAndStartRound()
    {
        _ticker.ToggleReadyAll(true);
        Assert.That(_ticker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));
        await Server.WaitPost(() => _ticker.StartRound());
        await RunUntilSynced();
    }

    private void AssertJob(ProtoId<JobPrototype> job, NetUserId? user = null, bool isAntag = false)
    {
        user ??= Client.User!.Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(_ticker.PlayerGameStatuses[user.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));
        }

        var uid = Server.PlayerMan.SessionsDict.GetValueOrDefault(user.Value)?.AttachedEntity;
        Assert.That(SEntMan.EntityExists(uid));
        var mind = _sMindSystem.GetMind(uid!.Value);
        Assert.That(SEntMan.EntityExists(mind));
        Assert.That(_sJobSystem.MindTryGetJobId(mind, out var actualJob));
        Assert.That(actualJob, Is.EqualTo(job));
        Assert.That(_sRoleSystem.MindIsAntagonist(mind), Is.EqualTo(isAntag));
    }

    public override async Task DoTeardown()
    {
        await Server.WaitPost(_ticker.RestartRound);
        await base.DoTeardown();
    }

    public override async Task DoSetup()
    {
        await base.DoSetup();
        await AssertInLobbyNotReady();
    }
}
