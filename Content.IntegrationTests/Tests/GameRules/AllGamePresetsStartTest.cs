using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Shuttles.Components;
using Content.Shared.Antag;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class AllGamePresetsStartTest : GameTest
{
    /// <summary>
    /// A list of blacklisted <see cref="GamePresetPrototype"/> for this test. Some down streams might make changes which nuke upstream game modes they don't use.
    /// This prevents them from being tested. If you use this to silence valid test fails and your game fails to start. Skill issue. Do 100 push-ups.
    /// </summary>
    private static readonly HashSet<string> IgnoredPresets = []; // Is a string to prevent YAML Linter from freaking if this is empty.

    private static string[] _gamePresets = GameDataScrounger.PrototypesOfKind<GamePresetPrototype>().Where(p => !IgnoredPresets.Contains(p)).ToArray();

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    // Tests that all game modes can start given ideal circumstances.
    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent))]
    [TestCaseSource(nameof(_gamePresets))]
    [Description("Ensures all Game Presets are able to start and assign all antags correctly without spawning anyone in nullspace.")]
    public async Task TestAllGamemodesCanStart(string presetId)
    {
        var server = Pair.Server;
        var client = Pair.Client;
        var protoMan = server.ProtoMan;
        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();
        var antagSys = server.System<AntagSelectionSystem>();
        var mind = server.System<SharedMindSystem>();

        // Initially in the lobby
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        // Don't start dummy antag game rule because we want our antags to be predictable for the test.
        server.CfgMan.SetCVar(CCVars.GameTickerIgnoredPresets, GameTicker.DummyGameRule);

        var preset = protoMan.Index<GamePresetPrototype>(presetId);

        // Spawn the minimum number of players.
        var players = new List<ICommonSession>();
        players.Add(client.Session);
        var min = 0;
        await server.WaitPost(() =>
        {
            min = ticker.GetMinimumPlayerCount(preset);
        });

        // We should already have one client connected, and we need to check the min

        // If we have antags, make sure that those with the correct preferences can spawn with them!
        List<(AntagSpecifierPrototype, int)> rules = [];

        var antags = 0;
        await server.WaitPost(() =>
        {
            foreach (var ruleId in preset.Rules)
            {
                if (ruleId == GameTicker.DummyGameRule)
                    continue;

                if (!protoMan.Resolve(ruleId, out var rule ))
                    continue; // Bruh moment

                // Ignore non-antag game-rules.
                if (!rule.TryGetComponent<AntagSelectionComponent>(out var antag, entMan.ComponentFactory))
                    continue;

                var runningCount = 0;

                foreach (var selector in antag.Antags)
                {
                    // Throw on invalid prototypes, skip roundstart ghost roles.
                    if (!protoMan.Resolve(selector.Proto, out var definition) || definition.PrefRoles.Count == 0)
                        continue;

                    var count = antagSys.GetTargetAntagCount(selector, min, ref runningCount);
                    antags += count;
                    rules.Add((definition, count));
                }
            }
        });

        // No preset should ever try to spawn more antags roundstart than it can spawn players.
        Assert.That(antags <= min, Is.True);
        if (min > 1)
        {
            var dummies = await server.AddDummySessions(min - 1);
            // Put our client at the front of the list.
            players = players.Union(dummies).ToList();
        }

        await Pair.RunUntilSynced();

        // This also ensures that admin commands work properly :P
        await server.WaitPost(() =>
        {
            ticker.ToggleReadyAll(true);
        });

        var i = 0;
        foreach (var (antag, amount) in rules)
        {
            for (var count = 0; count < amount; count++)
            {
                await Pair.SetAntagPreference(antag.PrefRoles.FirstOrDefault(), true, players[i++].UserId);
                Assert.That(i < min, $"Tried to assign more antags than there were players");
            }
        }

        await Pair.RunUntilSynced();
        await Pair.WaitCommand($"setgamepreset {presetId}");
        await Pair.WaitCommand("startround");
        await Pair.RunUntilSynced();

        // Game should have started
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
        Assert.That(ticker.PlayerGameStatuses.Count == players.Count);
        Assert.That(client.EntMan.EntityExists(client.AttachedEntity));

        var player = Pair.Player!.AttachedEntity!.Value;
        Assert.That(entMan.EntityExists(player));

        // Start all game presets so antags spawn!
        await server.WaitPost(() =>
        {
            ticker.StartGamePresetRules();
        });
        await Pair.RunUntilSynced();

        await server.WaitPost(() =>
        {
            var j = 0;
            foreach (var (antag, amount) in rules)
            {
                for (var count = 0; count < amount; count++)
                {
                    AssertAntagInitialized(antag, players[j++]);
                }
            }
        });

        // Maps now exist
        Assert.That(entMan.Count<MapComponent>(), Is.GreaterThan(0));
        Assert.That(entMan.Count<MapGridComponent>(), Is.GreaterThan(0));
        Assert.That(entMan.Count<StationCentcommComponent>(), Is.EqualTo(1));

        await Pair.WaitCommand("golobby");
        await Pair.RunUntilSynced();
        void AssertAntagInitialized(AntagSpecifierPrototype antag, ICommonSession session)
        {
            Assert.That(mind.TryGetMind(session, out var mindEnt, out var mindComp),
                $"Session {session} spawned into the game as an antag but had no mind!");
            Assert.That(entMan.EntityExists(mindComp!.CurrentEntity),
                $"Session {session} spawned into the game as an antag, but had no entity!");
            var ent = mindComp.CurrentEntity!.Value;

            // Make sure all components were added
            foreach (var comp in antag.Components)
            {
                Assert.That(entMan.HasComponent(ent, comp.Value.Component.GetType()),
                    $"Entity {entMan.ToPrettyString(ent)} owned by {session} failed to acquire {comp.Key} component, while becoming {antag.ID}");
            }

            // Make sure all mind components were added
            foreach (var comp in antag.MindComponents)
            {
                Assert.That(entMan.HasComponent(mindEnt, comp.Value.Component.GetType()),
                    $"Mind {entMan.ToPrettyString(mindEnt)} owned by {session} failed to acquire {comp.Key} component, while becoming {antag.ID}");
            }

            if (antag.MindRoles != null)
            {
                Assert.Multiple(() =>
                {
                    foreach (var role in antag.MindRoles)
                    {
                        Assert.That(mindComp!.MindRoleContainer.ContainedEntities.Any(x => entMan.MetaQuery.Comp(x).EntityPrototype?.ID == role),
                            $"{SToPrettyString(mindEnt)} owned by {session}, failed to acquire role {role} for antagonist {antag}");
                    }
                });
            }
        }
    }
}
