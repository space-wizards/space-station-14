#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Execution;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Commands;

public sealed class SuicideCommandTests : GameTest
{
    private const string SharpTestObject = "SharpTestObject";
    private const string MixedDamageTestObject = "MixedDamageTestObject";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {SharpTestObject}
  name: very sharp test object
  components:
  - type: Item
  - type: MeleeWeapon
    damage:
      types:
        Slash: 5
  - type: Execution

- type: entity
  id: {MixedDamageTestObject}
  name: mixed damage test object
  components:
  - type: Item
  - type: MeleeWeapon
    damage:
      types:
        Slash: 5
        Blunt: 5
  - type: Execution
";

    private static readonly ProtoId<TagPrototype> CannotSuicideTag = "CannotSuicide";
    private static readonly ProtoId<DamageTypePrototype> DamageType = "Slash";

    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        Dirty = true,
        DummyTicker = false
    };

    [SidedDependency(Side.Server)] private DamageableSystem _sDamageableSystem = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _sHandsSystem = default!;
    [SidedDependency(Side.Server)] private SharedMindSystem _sMindSystem = default!;
    [SidedDependency(Side.Server)] private MobStateSystem _sMobStateSystem = default!;
    [SidedDependency(Side.Server)] private TagSystem _sTagSystem = default!;

    /// <summary>
    /// Run the suicide command in the console.
    /// Should successfully kill the player and ghost them.
    /// </summary>
    [Test]
    public async Task TestSuicide()
    {
        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = ServerSession!.AttachedEntity!.Value;

        var mind = _sMindSystem.GetMind(player);
        Assume.That(mind, Is.Not.Null);

        var mindComponent = SComp<MindComponent>(mind.Value);
        var mobStateComp = SComp<MobStateComponent>(player);

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        await Pair.WaitClientCommand("suicide", 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sMobStateSystem.IsDead(player, mobStateComp));
            Assert.That(STryComp<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                        !ghostComp.CanReturnToBody);
        }
    }

    /// <summary>
    /// Run the suicide command while the player is already injured.
    /// This should only deal as much damage as necessary to get to the dead threshold.
    /// </summary>
    [Test]
    public async Task TestSuicideWhileDamaged()
    {
        var player = ServerSession!.AttachedEntity!.Value;
        var mind = _sMindSystem.GetMind(player);
        Assume.That(mind, Is.Not.Null);

        var mindComponent = SComp<MindComponent>(mind.Value);
        var mobStateComp = SComp<MobStateComponent>(player);
        var mobThresholdsComp = SComp<MobThresholdsComponent>(player);
        var slashProto = SProtoMan.Index(DamageType);

        await Server.WaitPost(() =>
        {
            _sDamageableSystem.TryChangeDamage(player, new DamageSpecifier(slashProto, FixedPoint2.New(46.5)));
        });

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        // and that all the damage is concentrated in the Slash category
        await Pair.WaitClientCommand("suicide", 1);
        await Server.WaitAssertion(() =>
        {
            var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sMobStateSystem.IsDead(player, mobStateComp));
                Assert.That(STryComp<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
                Assert.That(_sDamageableSystem.GetTotalDamage(player), Is.EqualTo(lethalDamageThreshold));
            }
        });
    }

    /// <summary>
    /// Run the suicide command in the console.
    /// Should only ghost the player but not kill them.
    /// </summary>
    [Test]
    public async Task TestSuicideWhenCannotSuicide()
    {
        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = ServerSession!.AttachedEntity!.Value;
        var mind = _sMindSystem.GetMind(player);
        Assume.That(mind, Is.Not.Null);

        var mindComponent = SComp<MindComponent>(mind.Value);
        var mobStateComp = SComp<MobStateComponent>(player);

        _sTagSystem.AddTag(player, CannotSuicideTag);

        // Check that running the suicide command ghosts but does not kill the player
        await Pair.WaitClientCommand("suicide", 1);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sMobStateSystem.IsAlive(player, mobStateComp));
                Assert.That(STryComp<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
            }
        });
    }


    /// <summary>
    /// Run the suicide command while the player is holding an execution-capable weapon
    /// </summary>
    [Test]
    public async Task TestSuicideByHeldItem()
    {
        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = ServerSession!.AttachedEntity!.Value;
        var mind = _sMindSystem.GetMind(player);
        Assume.That(mind, Is.Not.Null);

        var mindComponent = SComp<MindComponent>(mind.Value);
        var mobStateComp = SComp<MobStateComponent>(player);
        var mobThresholdsComp = SComp<MobThresholdsComponent>(player);
        var damageableComp = SComp<DamageableComponent>(player);
        var handsComponent = SComp<HandsComponent>(player);

        // Spawn the weapon of choice and put it in the player's hands
        await Server.WaitAssertion(() =>
        {
            var item = SSpawnAtPosition(SharpTestObject, SComp<TransformComponent>(player).Coordinates);
            Assert.That(_sHandsSystem.TryPickup(player, item, handsComponent.ActiveHandId));
            Assert.That(item, Has.Comp<ExecutionComponent>(Server));
        });

        await Server.WaitAssertion(() =>
        {
            // Heal all damage first (possible low pressure damage taken)
            _sDamageableSystem.ClearAllDamage((player, damageableComp));
        });

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        // and that all the damage is concentrated in the Slash category
        await Pair.WaitClientCommand("suicide", 1);
        await Server.WaitAssertion(() =>
        {
            var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sMobStateSystem.IsDead(player, mobStateComp));
                Assert.That(STryComp<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
                Assert.That(_sDamageableSystem.GetAllDamage((player, damageableComp)).DamageDict["Slash"], Is.EqualTo(lethalDamageThreshold));
            }
        });
    }

    /// <summary>
    /// Run the suicide command while the player is holding an execution-capable weapon
    /// with damage spread between slash and blunt
    /// </summary>
    [Test]
    public async Task TestSuicideByHeldItemSpreadDamage()
    {
        var player = ServerSession!.AttachedEntity!.Value;
        var mind = _sMindSystem.GetMind(player);
        Assume.That(mind, Is.Not.Null);

        var mindComponent = SComp<MindComponent>(mind.Value);
        var mobStateComp = SComp<MobStateComponent>(player);
        var mobThresholdsComp = SComp<MobThresholdsComponent>(player);
        var damageableComp = SComp<DamageableComponent>(player);
        var handsComponent = SComp<HandsComponent>(player);

        // Spawn the weapon of choice and put it in the player's hands
        await Server.WaitAssertion(() =>
        {
            var item = SSpawnAtPosition(MixedDamageTestObject, SComp<TransformComponent>(player).Coordinates);
            Assert.That(_sHandsSystem.TryPickup(player, item, handsComponent.ActiveHandId));
            Assert.That(item, Has.Comp<ExecutionComponent>(Server));
        });

        await Server.WaitAssertion(() =>
        {
            // Heal all damage first (possible low pressure damage taken)
            _sDamageableSystem.ClearAllDamage((player, damageableComp));
        });

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        // and that slash damage is split in half
        await Pair.WaitClientCommand("suicide", 1);
        await Server.WaitAssertion(() =>
        {
            var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sMobStateSystem.IsDead(player, mobStateComp));
                Assert.That(STryComp<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
                Assert.That(_sDamageableSystem.GetAllDamage((player, damageableComp)).DamageDict["Slash"], Is.EqualTo(lethalDamageThreshold / 2));
            }
        });
    }
}
