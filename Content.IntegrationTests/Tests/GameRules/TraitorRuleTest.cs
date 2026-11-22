#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed class TraitorRuleTest : GameTest
{
    private static readonly EntProtoId TraitorGameRuleProtoId = "Traitor";
    private const string TraitorAntagRoleName = "Traitor";
    private static readonly ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";
    private static readonly ProtoId<NpcFactionPrototype> NanotrasenFaction = "NanoTrasen";

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    [SidedDependency(Side.Server)] private ServerGameTicker _sTicker = null!;
    [SidedDependency(Side.Server)] private MindSystem _sMindSystem = null!;
    [SidedDependency(Side.Server)] private RoleSystem _sRoleSystem = null!;
    [SidedDependency(Side.Server)] private NpcFactionSystem _sFactionSystem = null!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = null!;
    [SidedDependency(Side.Server)] private SharedContainerSystem _sContainerSystem = null!;

    [Test]
    public async Task TestTraitorObjectives()
    {
        // Look up the minimum player count and max total objective difficulty for the game rule
        var minPlayers = 1;
        var maxDifficulty = 0f;
        await Server.WaitAssertion(() =>
        {
            var gameRuleEnt = SProtoMan.Index(TraitorGameRuleProtoId);

            Assert.That(gameRuleEnt.TryComp<GameRuleComponent>(out var gameRule, SEntMan.ComponentFactory),
                $"Game rule entity {TraitorGameRuleProtoId} does not have a {nameof(GameRuleComponent)}!");

            Assert.That(gameRuleEnt.TryComp<AntagRandomObjectivesComponent>(out var randomObjectives, SEntMan.ComponentFactory),
                $"Game rule entity {TraitorGameRuleProtoId} does not have an {nameof(AntagRandomObjectivesComponent)}!");

            minPlayers = gameRule!.MinPlayers;
            maxDifficulty = randomObjectives!.MaxDifficulty;
        });

        using (Assert.EnterMultipleScope())
        {
            // Initially in the lobby
            Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(Client.AttachedEntity, Is.Null);
            Assert.That(_sTicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        }

        // Add enough dummy players for the game rule
        var dummies = await Server.AddDummySessions(minPlayers);
        await RunTicksSync(5);

        // Initially, the players have no attached entities
        Assert.That(Pair.Player?.AttachedEntity, Is.Null);
        Assert.That(dummies, Has.All.Property(nameof(ICommonSession.AttachedEntity)).Null);

        // Opt-in the player for the traitor role
        await Pair.SetAntagPreference(TraitorAntagRoleName, true);

        // Add the game rule
        TraitorRuleComponent? traitorRule = null!;
        await Server.WaitAssertion(() =>
        {
            var gameRuleEnt = _sTicker.AddGameRule(TraitorGameRuleProtoId);
            Assert.That(gameRuleEnt, Is.Not.Null);
            Assert.That(STryComp(gameRuleEnt, out traitorRule));

            // Ready up
            _sTicker.ToggleReadyAll(true);
            Assert.That(_sTicker.PlayerGameStatuses.Values, Is.All.EqualTo(PlayerGameStatus.ReadyToPlay));

            // Start the round
            _sTicker.StartRound();
            // Force traitor mode to start (skip the delay)
            _sTicker.StartGameRule(gameRuleEnt.Value!);
        });

        await RunTicksSync(10);

        using (Assert.EnterMultipleScope())
        {
            // Game should have started
            Assert.That(_sTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(_sTicker.PlayerGameStatuses.Values, Is.All.EqualTo(PlayerGameStatus.JoinedGame));
            Assert.That(CEntMan.EntityExists(Client.AttachedEntity));
        }

        // Check the player and dummies are spawned
        var dummyEnts = dummies.Select(x => x.AttachedEntity ?? default).ToArray();
        var player = Pair.Player!.AttachedEntity!.Value;
        Assert.That(SEntMan.EntityExists(player));
        Assert.That(dummyEnts, Has.All.Matches<EntityUid>(SEntMan.EntityExists));

        // Make sure the player is a traitor.
        var mind = _sMindSystem.GetMind(player)!.Value;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sRoleSystem.MindIsAntagonist(mind));
            Assert.That(_sFactionSystem.IsMember(player, SyndicateFaction), Is.True);
            Assert.That(_sFactionSystem.IsMember(player, NanotrasenFaction), Is.False);
            Assert.That(traitorRule.TotalTraitors, Is.EqualTo(1));
            Assert.That(traitorRule.TraitorMinds[0], Is.EqualTo(mind));
        }

        // Check total objective difficulty
        var mindComp = SComp<MindComponent>(mind);
        var totalDifficulty = mindComp.Objectives.Sum(o => SComp<ObjectiveComponent>(o).Difficulty);
        Assert.That(totalDifficulty, Is.AtMost(maxDifficulty),
            $"MaxDifficulty exceeded! Objectives: {string.Join(", ", mindComp.Objectives.Select(o => FormatObjective(o, SEntMan)))}");
        Assert.That(mindComp.Objectives, Is.Not.Empty,
            $"No objectives assigned!");
    }

    private static readonly EntProtoId KillRandomHeadObjective = "KillRandomHeadObjective";
    private static readonly EntProtoId KillRandomPersonObjective = "KillRandomPersonObjective";
    private static readonly EntProtoId KillStationAiObjective = "KillStationAiObjective";
    private static readonly EntProtoId MindRoleTraitor = "MindRoleTraitor";
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId PlayerStationAiEmpty = "PlayerStationAiEmpty";
    private static readonly EntProtoId StationAiBrain = "StationAiBrain";
    private static readonly ProtoId<JobPrototype> PassengerJob = "Passenger";
    private static readonly ProtoId<JobPrototype> CaptainJob = "Captain";
    private static readonly ProtoId<JobPrototype> StationAiJob = "StationAi";

    [Test]
    [RunOnSide(Side.Server)]
    [Description("Tests that the game can properly assign kill objectives")]
    public async Task TestTraitorKillObjectiveTargets()
    {
        var map = _sMapSystem.CreateMap();
        var coordinates = new EntityCoordinates(map, System.Numerics.Vector2.Zero);

        var traitor = CreateMind(SSpawnAtPosition(MobHuman, coordinates), PassengerJob);
        _sRoleSystem.MindAddRole(traitor.Owner, MindRoleTraitor, silent: true);

        var captainBody = SSpawnAtPosition(MobHuman, coordinates);
        SEntMan.EnsureComponent<CommandStaffComponent>(captainBody);
        var captain = CreateMind(captainBody, CaptainJob);

        var tider = CreateMind(SSpawnAtPosition(MobHuman, coordinates), PassengerJob);

        var core = SSpawnAtPosition(PlayerStationAiEmpty, coordinates);
        var brain = SSpawnAtPosition(StationAiBrain, coordinates);
        var container = _sContainerSystem.GetContainer(core, StationAiHolderComponent.Container);
        Assume.That(_sContainerSystem.Insert(brain, container), Is.True, "Could not insert the AI into its core.");
        var ai = CreateMind(brain, StationAiJob);

        //Assign head first because we can always expect that to be captain given the options
        Assert.That(_sMindSystem.TryAddObjective(traitor.Owner, traitor.Comp, KillRandomHeadObjective), Is.True,
            $"Could not assign {KillRandomHeadObjective}.");
        Assert.That(SComp<TargetObjectiveComponent>(traitor.Comp.Objectives[0]).Target, Is.EqualTo(captain.Owner),
            $"{KillRandomHeadObjective} selected the wrong target.");


        //Tider is only remaining valid target for a kill person objective.
        Assert.That(_sMindSystem.TryAddObjective(traitor.Owner, traitor.Comp, KillRandomPersonObjective), Is.True,
            $"Could not assign {KillRandomPersonObjective}.");
        Assert.That(SComp<TargetObjectiveComponent>(traitor.Comp.Objectives[1]).Target, Is.EqualTo(tider.Owner),
            $"{KillRandomPersonObjective} selected the wrong target.");

        //Also check AI, comment because it feels weird having them for the other two and not this
        Assert.That(_sMindSystem.TryAddObjective(traitor.Owner, traitor.Comp, KillStationAiObjective), Is.True,
            $"Could not assign {KillStationAiObjective}.");
        Assert.That(SComp<TargetObjectiveComponent>(traitor.Comp.Objectives[2]).Target, Is.EqualTo(ai.Owner),
            $"{KillStationAiObjective} selected the wrong target.");
    }

    private Entity<MindComponent> CreateMind(EntityUid body, ProtoId<JobPrototype> job)
    {
        var mind = _sMindSystem.CreateMind(null);
        _sMindSystem.TransferTo(mind.Owner, body);
        _sRoleSystem.MindAddJobRole(mind.Owner, jobPrototype: job, silent: true);
        return mind;
    }

    private static string FormatObjective(EntityUid entity, IEntityManager entMan)
    {
        var meta = entMan.GetComponent<MetaDataComponent>(entity);
        var objective = entMan.GetComponent<ObjectiveComponent>(entity);
        return $"{meta.EntityName} ({objective.Difficulty})";
    }
}
