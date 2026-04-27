using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Antag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class GhostRoleTest : GameTest
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [SidedDependency(Side.Server)] private readonly GameTicker _ticker = default!;
    [SidedDependency(Side.Server)] private readonly GhostRoleSystem _ghostRole = default!;

    private static string[] _gameRules = GameDataScrounger.EntitiesWithComponent("AntagSelection");

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        Map = PoolManager.TestStation
    };

    // Tests that all game modes can start given ideal circumstances.
    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent)), TestOf(typeof(GhostRoleSystem))]
    [TestCaseSource(nameof(_gameRules))]
    [Description("Ensures all GameRule entities with ghost roles can properly spawn those roles and they can be taken.")]
    public async Task TestGhostRolesAssignment(string ruleId)
    {
        await Server.WaitIdleAsync();

        AntagSelectionComponent antag = null;
        await Server.WaitAssertion(() =>
        {
            var rule = SProtoMan.Index<EntityPrototype>(ruleId);
            Assert.That(rule.TryGetComponent(out antag));
        });

        var gameRule = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            _ticker.StartGameRule(ruleId, out gameRule);
        });

        Dictionary<ProtoId<AntagSpecifierPrototype>, int> rules = [];

        await Server.WaitAssertion(() =>
        {
            foreach (var selector in antag!.Antags)
            {
                var specifier = SProtoMan.Index(selector.Proto);
                var count = selector.GetTargetAntagCount(_random, 1);
                // We should always spawn at leastone antag if we add a GameRule
                Assert.That(count > 0);

                if (specifier.SpawnerPrototype == null)
                    continue;

                var value = rules.GetValueOrDefault(specifier);
                rules[selector.Proto] = value + count;
            }
        });

        await Server.WaitAssertion(() =>
        {
            var roleEnumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent>();
            while (roleEnumerator.MoveNext(out var spawner, out var role))
            {
                Assert.That(spawner.Rule, Is.EqualTo(gameRule));
                Assert.That(spawner.Definition.HasValue);
                var value = rules[spawner.Definition!.Value];
                rules[spawner.Definition!.Value] = value - 1;

                // Take the ghost role and ensure we take it!
                Assert.That(_ghostRole.Takeover(ServerSession, role.Identifier));
            }

            // Ensure all ghost roles spawned and were assigned!!!
            foreach (var (_, count) in rules)
            {
                Assert.That(count, Is.EqualTo(0));
            }
        });

        await Server.WaitAssertion(() =>
        {
            // End all rules
            _ticker.ClearGameRules();
            Assert.That(!_ticker.GetAddedGameRules().Any());
        });
    }
}
