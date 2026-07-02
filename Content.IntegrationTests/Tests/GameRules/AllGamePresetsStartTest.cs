using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Shuttles.Components;
using Content.Shared.Antag;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class AllGamePresetsStartTest : AntagTest
{
    /// <summary>
    /// A list of blacklisted <see cref="GamePresetPrototype"/> for this test. Some down streams might make changes which nuke upstream game modes they don't use.
    /// This prevents them from being tested. If you use this to silence valid test fails and your game fails to start. Skill issue. Do 100 push-ups.
    /// </summary>
    private static readonly HashSet<string> IgnoredPresets = []; // Is a string to prevent YAML Linter from freaking if this is empty.

    private static string[] _gamePresets = GameDataScrounger.PrototypesOfKind<GamePresetPrototype>().Where(p => !IgnoredPresets.Contains(p)).ToArray();

    // Tests that all game modes can start given ideal circumstances.
    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent))]
    [TestCaseSource(nameof(_gamePresets))]
    [Description("Ensures all Game Presets are able to start and assign all antags correctly without spawning anyone in nullspace.")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameTickerIgnoredPresets), GameTicker.DummyGameRule)]
    public async Task TestAllGamemodesCanStart(string presetId)
    {
        // Initially in the lobby
        await Server.WaitPost(() =>
        {
            Assert.That(STicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(Client.AttachedEntity, Is.Null);
            Assert.That(STicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        });

        var preset = SProtoMan.Index<GamePresetPrototype>(presetId);

        // Spawn the minimum number of players.
        var players = new List<ICommonSession>();
        players.Add(Client.Session);
        var min = 0;
        await Server.WaitPost(() =>
        {
            min = STicker.GetMinimumPlayerCount(preset);
        });

        // We should already have one client connected, and we need to check the min

        // If we have antags, make sure that those with the correct preferences can spawn with them!
        List<(AntagSpecifierPrototype, int)> rules = [];

        var antags = 0;
        await Server.WaitPost(() =>
        {
            foreach (var ruleId in preset.Rules)
            {
                if (STicker.IsIgnored(ruleId))
                    continue;

                if (!SProtoMan.Resolve(ruleId, out var rule ))
                    continue; // Bruh moment

                // Ignore non-antag game-rules.
                if (!rule.TryComp<AntagSelectionComponent>(out var antag, SEntMan.ComponentFactory))
                    continue;

                var runningCount = 0;

                foreach (var selector in antag.Antags)
                {
                    // Throw on invalid prototypes, skip roundstart ghost roles.
                    if (!SProtoMan.Resolve(selector.Proto, out var definition) || definition.PrefRoles.Count == 0)
                        continue;

                    var count = AntagSys.GetTargetAntagCount(selector, min, ref runningCount);
                    antags += count;
                    rules.Add((definition, count));
                }
            }
        });

        // No preset should ever try to spawn more antags roundstart than it can spawn players.
        Assert.That(antags <= min, Is.True);
        if (min > 1)
        {
            var dummies = await Server.AddDummySessions(min - 1);
            // Put our client at the front of the list.
            players = players.Union(dummies).ToList();
        }

        await Pair.RunUntilSynced();

        // This also ensures that admin commands work properly :P
        await Server.WaitPost(() =>
        {
            STicker.ToggleReadyAll(true);
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
        await Server.WaitPost(() =>
        {
            Assert.That(STicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(STicker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
            Assert.That(STicker.PlayerGameStatuses, Has.Count.EqualTo(players.Count));
        });
        Assert.That(CEntMan.EntityExists(Client.AttachedEntity));

        var player = Pair.Player!.AttachedEntity!.Value;
        Assert.That(SEntMan.EntityExists(player));

        // Start all game presets so antags spawn!
        await Server.WaitPost(() =>
        {
            STicker.StartGamePresetRules();
        });
        await Pair.RunUntilSynced();

        await Server.WaitPost(() =>
        {
            var j = 0;
            foreach (var (antag, amount) in rules)
            {
                for (var count = 0; count < amount; count++)
                {
                    SAssertAntagInitialized(antag, players[j++]);
                }
            }
        });

        // Maps now exist
        Assert.That(SEntMan.Count<MapComponent>(), Is.GreaterThan(0));
        Assert.That(SEntMan.Count<MapGridComponent>(), Is.GreaterThan(0));
        Assert.That(SEntMan.Count<StationCentcommComponent>(), Is.EqualTo(1));

        // Clear game preset and return to lobby
        await Pair.WaitCommand("golobby");
        STicker.SetGamePreset((GamePresetPrototype) null);
        await Pair.RunUntilSynced();
    }
}
