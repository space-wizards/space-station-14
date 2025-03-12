using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class TraitorRuleTest
{
    private const string TraitorGameRuleProtoId = "Traitor";
    private const string TraitorAntagRoleName = "Traitor";

    [Test]
    public async Task TestTraitorObjectives()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings()
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFact = server.ResolveDependency<IComponentFactory>();
        var ticker = server.System<GameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();
        var factionSys = server.System<NpcFactionSystem>();
        var traitorRuleSys = server.System<TraitorRuleSystem>();

        // Look up the minimum player count and max total objective difficulty for the game rule
        var minPlayers = 1;
        var maxDifficulty = 0f;
        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex<EntityPrototype>(TraitorGameRuleProtoId, out var gameRuleEnt),
            $"Failed to lookup traitor game rule entity prototype with ID \"{TraitorGameRuleProtoId}\"!");

            Assert.That(gameRuleEnt.TryGetComponent<GameRuleComponent>(out var gameRule, compFact),
            $"Game rule entity {TraitorGameRuleProtoId} does not have a GameRuleComponent!");

            Assert.That(gameRuleEnt.TryGetComponent<AntagRandomObjectivesComponent>(out var randomObjectives, compFact),
            $"Game rule entity {TraitorGameRuleProtoId} does not have an AntagRandomObjectivesComponent!");

            minPlayers = gameRule.MinPlayers;
            maxDifficulty = randomObjectives.MaxDifficulty;
        });

        // Initially in the lobby
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        // Add enough dummy players for the game rule
        var dummies = await pair.Server.AddDummySessions(minPlayers);
        await pair.RunTicksSync(5);

        // Initially, the players have no attached entities
        Assert.That(pair.Player?.AttachedEntity, Is.Null);
        Assert.That(dummies.All(x => x.AttachedEntity == null));

        // Opt-in the player for the traitor role
        await pair.SetAntagPreference(TraitorAntagRoleName, true);

        // Add the game rule
        TraitorRuleComponent traitorRule = null;
        await server.WaitPost(() =>
        {
            var gameRuleEnt = ticker.AddGameRule(TraitorGameRuleProtoId);
            Assert.That(entMan.TryGetComponent<TraitorRuleComponent>(gameRuleEnt, out traitorRule));

            // Ready up
            ticker.ToggleReadyAll(true);
            Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.ReadyToPlay));

            // Start the round
            ticker.StartRound();
            // Force traitor mode to start (skip the delay)
            ticker.StartGameRule(gameRuleEnt);
        });
        await pair.RunTicksSync(10);

        // Game should have started
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
        Assert.That(client.EntMan.EntityExists(client.AttachedEntity));

        // Check the player and dummies are spawned
        var dummyEnts = dummies.Select(x => x.AttachedEntity ?? default).ToArray();
        var player = pair.Player!.AttachedEntity!.Value;
        Assert.That(entMan.EntityExists(player));
        Assert.That(dummyEnts.All(entMan.EntityExists));

        // Make sure the player is a traitor.
        var mind = mindSys.GetMind(player)!.Value;
        Assert.That(roleSys.MindIsAntagonist(mind));
        Assert.That(factionSys.IsMember(player, "Syndicate"), Is.True);
        Assert.That(factionSys.IsMember(player, "NanoTrasen"), Is.False);
        Assert.That(traitorRule.TotalTraitors, Is.EqualTo(1));
        Assert.That(traitorRule.TraitorMinds[0], Is.EqualTo(mind));

        // Check total objective difficulty
        Assert.That(entMan.TryGetComponent<MindComponent>(mind, out var mindComp));
        var totalDifficulty = mindComp.Objectives.Sum(o => entMan.GetComponent<ObjectiveComponent>(o).Difficulty);
        Assert.That(totalDifficulty, Is.AtMost(maxDifficulty),
            $"MaxDifficulty exceeded! Objectives: {string.Join(", ", mindComp.Objectives.Select(o => FormatObjective(o, entMan)))}");
        Assert.That(mindComp.Objectives, Is.Not.Empty,
            $"No objectives assigned!");


        await pair.CleanReturnAsync();
    }

    private static string FormatObjective(Entity<ObjectiveComponent> entity, IEntityManager entMan)
    {
        var meta = entMan.GetComponent<MetaDataComponent>(entity);
        var objective = entMan.GetComponent<ObjectiveComponent>(entity);
        return $"{meta.EntityName} ({objective.Difficulty})";
    }
}
