using System.Collections.Generic;
using System.Linq;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Shuttles.Components;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class AllGamemodesStartTest
{
    /// <summary>
    /// A list of blacklisted gamemodes for this test. Some downstreams might make changes which nuke upstream game modes they don't use.
    /// This prevents them from being tested. If you use this to silence valid test fails and your game fails to start. Skill issue. Do 100 push-ups.
    /// </summary>
    private static readonly HashSet<ProtoId<GamePresetPrototype>> BlacklistedPresets = [];

    // Tests that all game modes can start given ideal circumstances.
    [Test]
    public async Task TestAllGamemodesCanStart()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        var server = pair.Server;
        var client = pair.Client;
        var protoMan = server.ProtoMan;
        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();
        var antagSys = server.System<AntagSelectionSystem>();
        var mind = server.System<SharedMindSystem>();

        // Get all current game presets
        foreach (var preset in protoMan.EnumeratePrototypes<GamePresetPrototype>())
        {
            if (BlacklistedPresets.Contains(preset))
                continue;

            // Initially in the lobby
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(client.AttachedEntity, Is.Null);
            Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

            // Spawn the minimum number of players.
            var min = ticker.GetMinimumPlayerCount(preset);
            var dummies = await pair.Server.AddDummySessions(min - 1); // We should already have one client connected, and we need to check the min...

            // TODO: Might wanna keep the dummies separate?
            dummies[min - 1] = dummies[0];
            dummies[0] = client.Session; // Ensure our client gets picked for antag first!

            // If we have antags, make sure that those with the correct preferences can spawn with them!
            var rules = GetAntagRules(preset, min).ToArray();

            var i = 0;
            foreach (var (antag, amount) in rules)
            {
               for (var count = 0; count < amount; count++)
               {
                   await pair.SetAntagPreference(antag.PrefRoles.FirstOrDefault(), true, dummies[i].UserId);
                   i++;
                   Assert.That(i < min, "Tried to assign more antags than there were players.");
               }
            }

            // This also ensures that admin commands work properly :P
            ticker.ToggleReadyAll(true);
            await pair.WaitCommand($"setgamepreset {preset.ID}");
            await pair.WaitCommand("startround");
            await pair.RunTicksSync(10);

            // Game should have started
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
            Assert.That(client.EntMan.EntityExists(client.AttachedEntity));

            var player = pair.Player!.AttachedEntity!.Value;
            Assert.That(entMan.EntityExists(player));

            List<EntityUid> dummyEnts = [];
            for (var j = 0; j < dummies.Length; j++)
            {
                var session = dummies[j];
                Assert.That(entMan.EntityExists(session.AttachedEntity), $"Session {j} did not spawn with a valid entity during game preset {preset.ID}");
                var ent = session.AttachedEntity!.Value;
                dummyEnts.Add(ent);

                // We should have an assigned antag, so lets check!
                if (j <= i)
                {
                    var index = 0;
                    foreach (var (antag, amount) in rules)
                    {
                        if (amount + index < j + 1)
                            index += amount;
                        else
                        {
                            // Make sure all components were added
                            foreach (var comp in antag.Components)
                            {
                                Assert.That(entMan.HasComponent(ent, comp.Value.Component.GetType()));
                            }

                            var mindEnt = mind.GetMind(ent);
                            Assert.That(mindEnt != null, $"Session {j} spawned into the game as an antag {antag.ID}, but had no mind");
                            Assert.That(entMan.TryGetComponent<MindComponent>(mindEnt, out var mindComp));

                            // Make sure all mind components were added
                            foreach (var comp in antag.MindComponents)
                            {
                                Assert.That(entMan.HasComponent(ent, comp.Value.Component.GetType()));
                            }

                            if (antag.MindRoles != null)
                                Assert.That(mindComp!.MindRoleContainer.ContainedEntities.Select(x => antag.MindRoles.Contains(entMan.MetaQuery.Comp(x).EntityPrototype?.ID)).Count() == antag.MindRoles.Count);
                        }
                    }
                }
            }
            Assert.That(dummyEnts.All(e => entMan.EntityExists(e)));

            // Maps now exist
            Assert.That(entMan.Count<MapComponent>(), Is.GreaterThan(0));
            Assert.That(entMan.Count<MapGridComponent>(), Is.GreaterThan(0));
            Assert.That(entMan.Count<StationCentcommComponent>(), Is.EqualTo(1));

            // TODO: Reset round, disconnect players...
            await pair.WaitCommand($"golobby");
            await pair.RunTicksSync(10);
            await pair.Server.RemoveAllDummySessions();
            ticker.SetGamePreset((GamePresetPrototype) null);
        }

        await pair.CleanReturnAsync();

        // Similar to an AntagRule list but without the entities since we don't want to spawn them yet...
        IEnumerable<(AntagSpecifierPrototype, int)> GetAntagRules(GamePresetPrototype preset, int playerCount)
        {
            foreach (var ruleId in preset.Rules)
            {
                if (!protoMan.Resolve(ruleId, out var rule ))
                    continue; // Bruh moment

                // Ignore non-antag game-rules.
                if (!rule.TryGetComponent<AntagSelectionComponent>(out var antag, entMan.ComponentFactory))
                    continue;

                var runningCount = 0;

                foreach (var proto in antag.Antags)
                {
                    if (!protoMan.Resolve(proto, out var definition))
                        continue;

                    yield return (definition, antagSys.GetTargetAntagCount(definition, playerCount, ref runningCount));
                }
            }
        }
    }
}
