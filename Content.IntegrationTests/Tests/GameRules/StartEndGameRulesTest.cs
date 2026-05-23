using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class StartEndGameRulesTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings
    {
        Dirty = true,
        DummyTicker = false,
        Map = PoolManager.TestStation
    };

    /// <summary>
    ///     Tests that all game rules can be added/started/ended at the same time without exceptions.
    /// </summary>
    [Test]
    public async Task TestAllConcurrent()
    {
        var pair = Pair;
        var server = pair.Server;
        await server.WaitIdleAsync();
        var gameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
        var cfg = server.ResolveDependency<IConfigurationManager>();
        Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

        // get the server to control someone so that AntagLivingSpawn rules doesn't fail
        // spawn a body
        EntityUid target = default;
        await server.WaitAssertion(() => target = server.EntMan.Spawn("MobHuman"));
        // get dependencies
        var entMan = Pair.Server.ResolveDependency<IServerEntityManager>();
        var playerMan = Pair.Server.ResolveDependency<Robust.Server.Player.IPlayerManager>();
        var mindSys = entMan.System<Server.Mind.MindSystem>();
        var transformSys = entMan.System<SharedTransformSystem>();
        Entity<MindComponent> mind = default!;
        await Pair.Server.WaitPost(() =>
        {
            // make the "client" takeover the entity
            var player = entMan.GetNetEntity(target);
            mind = mindSys.CreateMind(ServerSession.UserId, "DummyPlayerEntity");
            var playerEnt = entMan.GetEntity(player);
            mindSys.TransferTo(mind, playerEnt, mind: mind.Comp);
            Server.PlayerMan.SetAttachedEntity(ServerSession, playerEnt);
        });

        await server.WaitAssertion(() =>
        {
            var rules = gameTicker.GetAllGameRulePrototypes().ToList();
            rules.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));

            // Start all rules
            foreach (var rule in rules)
            {
                gameTicker.StartGameRule(rule.ID);
            }
        });

        // Wait three ticks for any random update loops that might happen
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            // End all rules
            gameTicker.ClearGameRules();
            Assert.That(!gameTicker.GetAddedGameRules().Any());
        });
    }
}
