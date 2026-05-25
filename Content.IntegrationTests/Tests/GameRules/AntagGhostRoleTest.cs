#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Antag;
using Content.Shared.Players;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed partial class AntagGhostRoleTest : AntagTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        Map = PoolManager.TestStation
    };

    [SidedDependency(Side.Server)] private IRobustRandom _random = default!;
    [SidedDependency(Side.Server)] private GhostRoleSystem _ghostRole = default!;
    [SidedDependency(Side.Server)] private IEntityManager _entMan = default!;

    private static readonly string[] AntagGameRules = GameDataScrounger.EntitiesWithComponent("AntagSelection");

    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent)), TestOf(typeof(GhostRoleSystem))]
    [TestCaseSource(nameof(AntagGameRules))]
    [Description($"Ensures all GameRule entities with {nameof(AntagSelectionComponent)} can properly spawn those roles and they can be taken.")]
    [RunOnSide(Side.Server)]
    public void TestAntagGhostRoles(string ruleId)
    {
        var rule = SProtoMan.Index<EntityPrototype>(ruleId);
        Assert.That(rule.TryGetComponent<AntagSelectionComponent>(out var antag, SEntMan.ComponentFactory), Is.True);

        STicker.StartGameRule(ruleId, out var gameRule);

        Dictionary<ProtoId<AntagSpecifierPrototype>, int> rules = [];

        foreach (var selector in antag!.Antags)
        {
            var specifier = SProtoMan.Index(selector.Proto);
            var count = selector.GetTargetAntagCount(_random, 1);
            // We should always spawn at least one antag if we add a GameRule
            Assert.That(count, Is.GreaterThan(0));

            if (specifier.SpawnerPrototype == null)
                continue;

            var value = rules.GetValueOrDefault(specifier);
            rules[selector.Proto] = value + count;
        }

        var roleEnumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent, TransformComponent>();
        while (roleEnumerator.MoveNext(out var spawner, out var role, out var xform))
        {
            // Ensure the ghost role spawner spawned correctly!
            Assert.That(spawner.Rule, Is.EqualTo(gameRule));
            Assert.That(spawner.Definition, Is.Not.Null);
            AssertGhostRoleTaken(spawner, role, xform);
            var value = rules[spawner.Definition.Value];
            rules[spawner.Definition.Value] = value - 1;
        }

        // Ensure all ghost roles spawned and were assigned!!!
        Assert.That(rules.Values, Is.All.Zero);

        // End all rules
        STicker.ClearGameRules();
        Assert.That(STicker.GetAddedGameRules(), Is.Empty);
    }

    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent)), TestOf(typeof(GhostRoleSystem))]
    [Description("Ensures a player can take all antag ghost roles sequentially without transferring unwanted mind data.")]
    [RunOnSide(Side.Server)]
    public void TestAntagGhostRolesSequential()
    {
        foreach (var ruleId in AntagGameRules)
        {
            var rule = SProtoMan.Index<EntityPrototype>(ruleId);
            Assert.That(rule.TryGetComponent<AntagSelectionComponent>(out var antag, SEntMan.ComponentFactory), Is.True);
            STicker.StartGameRule(ruleId);
        }

        var mind = ServerSession!.GetMind();

        var roleEnumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent, TransformComponent>();
        while (roleEnumerator.MoveNext(out var spawner, out var role, out var xform))
        {
            AssertGhostRoleTaken(spawner, role, xform);
            var newMind = ServerSession!.GetMind();
            Assert.That(newMind, Is.Not.EqualTo(mind));
            mind = newMind;
        }

        // End all rules
        STicker.ClearGameRules();
        Assert.That(STicker.GetAddedGameRules(), Is.Empty);
    }

    private void AssertGhostRoleTaken(GhostRoleAntagSpawnerComponent spawner, GhostRoleComponent role, TransformComponent xform)
    {
        // Ensure the ghost role spawner spawned correctly!
        Assert.That(spawner.Definition, Is.Not.Null);
        Assert.That(xform.MapUid, Is.Not.Null);
        Assert.That(xform.MapID, Is.Not.EqualTo(MapId.Nullspace));
        Assert.That(xform.Coordinates.IsValid(_entMan), Is.True);

        // Take the ghost role and ensure we take it!
        Assert.That(_ghostRole.Takeover(ServerSession!, role.Identifier), Is.True);
        Assert.That(ServerSession!.AttachedEntity, Is.Not.Null);
        var antag = SProtoMan.Index(spawner.Definition);
        SAssertAntagInitialized(antag, ServerSession);

        // Ensure we spawned in the correct location
        var sessionXform = SEntMan.GetComponent<TransformComponent>(ServerSession.AttachedEntity.Value);

        // We do it via distance so that it works for ghost roles which don't spawn "exactly" at their spawn position (e.g. paradox clones which spawn in a container)
        var hadDistance = sessionXform.Coordinates.TryDistance(_entMan, xform.Coordinates, out var distance);
        Assert.That(hadDistance, Is.True);

        // I will not get heisentest due to floating point errors
        Assert.That(MathHelper.CloseTo(distance, 0f, 0.001f), Is.True);
    }
}
