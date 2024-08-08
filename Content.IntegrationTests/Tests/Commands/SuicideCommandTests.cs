using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Execution;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Commands;

[TestFixture]
public sealed class SuicideCommandTests
{

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: SharpTestObject
  name: very sharp test object
  components:
  - type: Item
  - type: MeleeWeapon
    damage:
      types:
        Slash: 5
  - type: Execution

- type: entity
  id: MixedDamageTestObject
  name: mixed damage test object
  components:
  - type: Item
  - type: MeleeWeapon
    damage:
      types:
        Slash: 5
        Blunt: 5
  - type: Execution

- type: entity
  id: TestMaterialReclaimer
  name: test version of the material reclaimer
  components:
  - type: MaterialReclaimer";

    /// <summary>
    /// Run the suicide command in the console
    /// Should successfully kill the player and ghost them
    /// </summary>
    [Test]
    public async Task TestSuicide()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false
        });
        var server = pair.Server;
        var consoleHost = server.ResolveDependency<IConsoleHost>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mindSystem = entManager.System<SharedMindSystem>();
        var mobStateSystem = entManager.System<MobStateSystem>();

        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = playerMan.Sessions.First().AttachedEntity!.Value;
        var mind = mindSystem.GetMind(player);

        MindComponent mindComponent = default;
        MobStateComponent mobStateComp = default;
        await server.WaitPost(() =>
        {
            if (mind != null)
                mindComponent = entManager.GetComponent<MindComponent>(mind.Value);

            mobStateComp = entManager.GetComponent<MobStateComponent>(player);
        });


        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        await server.WaitAssertion(() =>
        {
            consoleHost.GetSessionShell(playerMan.Sessions.First()).ExecuteCommand("suicide");
            Assert.Multiple(() =>
            {
                Assert.That(mobStateSystem.IsDead(player, mobStateComp));
                Assert.That(entManager.TryGetComponent<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Run the suicide command while the player is already injured
    /// This should only deal as much damage as necessary to get to the dead threshold
    /// </summary>
    [Test]
    public async Task TestSuicideWhileDamaged()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false
        });
        var server = pair.Server;
        var consoleHost = server.ResolveDependency<IConsoleHost>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var damageableSystem = entManager.System<DamageableSystem>();
        var mindSystem = entManager.System<SharedMindSystem>();
        var mobStateSystem = entManager.System<MobStateSystem>();

        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = playerMan.Sessions.First().AttachedEntity!.Value;
        var mind = mindSystem.GetMind(player);

        MindComponent mindComponent = default;
        MobStateComponent mobStateComp = default;
        MobThresholdsComponent mobThresholdsComp = default;
        DamageableComponent damageableComp = default;
        await server.WaitPost(() =>
        {
            if (mind != null)
                mindComponent = entManager.GetComponent<MindComponent>(mind.Value);

            mobStateComp = entManager.GetComponent<MobStateComponent>(player);
            mobThresholdsComp = entManager.GetComponent<MobThresholdsComponent>(player);
            damageableComp = entManager.GetComponent<DamageableComponent>(player);
        });

        if (protoMan.TryIndex<DamageTypePrototype>("Slash", out var slashProto))
            damageableSystem.TryChangeDamage(player, new DamageSpecifier(slashProto, FixedPoint2.New(46.5)));

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        // and that all the damage is concentrated in the Slash category
        await server.WaitAssertion(() =>
        {
            consoleHost.GetSessionShell(playerMan.Sessions.First()).ExecuteCommand("suicide");
            var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();

            Assert.Multiple(() =>
            {
                Assert.That(mobStateSystem.IsDead(player, mobStateComp));
                Assert.That(entManager.TryGetComponent<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
                Assert.That(damageableComp.Damage.GetTotal(), Is.EqualTo(lethalDamageThreshold));
            });
        });

        await pair.CleanReturnAsync();
    }

        /// <summary>
    /// Run the suicide command in the console
    /// Should only ghost the player but not kill them
    /// </summary>
    [Test]
    public async Task TestSuicideWhenCannotSuicide()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false
        });
        var server = pair.Server;
        var consoleHost = server.ResolveDependency<IConsoleHost>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mindSystem = entManager.System<SharedMindSystem>();
        var mobStateSystem = entManager.System<MobStateSystem>();
        var tagSystem = entManager.System<TagSystem>();

        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = playerMan.Sessions.First().AttachedEntity!.Value;
        var mind = mindSystem.GetMind(player);
        MindComponent mindComponent = default;
        MobStateComponent mobStateComp = default;
        await server.WaitPost(() =>
        {
            if (mind != null)
                mindComponent = entManager.GetComponent<MindComponent>(mind.Value);
            mobStateComp = entManager.GetComponent<MobStateComponent>(player);
        });

        tagSystem.AddTag(player, "CannotSuicide");

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        await server.WaitAssertion(() =>
        {
            consoleHost.GetSessionShell(playerMan.Sessions.First()).ExecuteCommand("suicide");
            Assert.Multiple(() =>
            {
                Assert.That(mobStateSystem.IsAlive(player, mobStateComp));
                Assert.That(entManager.TryGetComponent<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
            });
        });

        await pair.CleanReturnAsync();
    }


    /// <summary>
    /// Run the suicide command while the player is holding an execution-capable weapon
    /// </summary>
    [Test]
    public async Task TestSuicideByHeldItem()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false
        });
        var server = pair.Server;
        var consoleHost = server.ResolveDependency<IConsoleHost>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        var handsSystem = entManager.System<SharedHandsSystem>();
        var mindSystem = entManager.System<SharedMindSystem>();
        var mobStateSystem = entManager.System<MobStateSystem>();
        var transformSystem = entManager.System<TransformSystem>();

        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = playerMan.Sessions.First().AttachedEntity!.Value;
        var mind = mindSystem.GetMind(player);

        MindComponent mindComponent = default;
        MobStateComponent mobStateComp = default;
        MobThresholdsComponent mobThresholdsComp = default;
        DamageableComponent damageableComp = default;
        HandsComponent handsComponent = default;
        await server.WaitPost(() =>
        {
            if (mind != null)
                mindComponent = entManager.GetComponent<MindComponent>(mind.Value);

            mobStateComp = entManager.GetComponent<MobStateComponent>(player);
            mobThresholdsComp = entManager.GetComponent<MobThresholdsComponent>(player);
            damageableComp = entManager.GetComponent<DamageableComponent>(player);
            handsComponent = entManager.GetComponent<HandsComponent>(player);
        });

        // Spawn the weapon of choice and put it in the player's hands
        await server.WaitPost(() =>
        {
            var item = entManager.SpawnEntity("SharpTestObject", transformSystem.GetMapCoordinates(player));
            Assert.That(handsSystem.TryPickup(player, item, handsComponent.ActiveHand!));
            entManager.TryGetComponent<ExecutionComponent>(item, out var executionComponent);
            Assert.That(executionComponent, Is.Not.EqualTo(null));
        });

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        // and that all the damage is concentrated in the Slash category
        await server.WaitAssertion(() =>
        {
            consoleHost.GetSessionShell(playerMan.Sessions.First()).ExecuteCommand("suicide");
            var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();

            Assert.Multiple(() =>
            {
                Assert.That(mobStateSystem.IsDead(player, mobStateComp));
                Assert.That(entManager.TryGetComponent<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
                Assert.That(damageableComp.Damage.DamageDict["Slash"], Is.EqualTo(lethalDamageThreshold));
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Run the suicide command while the player is holding an execution-capable weapon
    /// with damage spread between slash and blunt
    /// </summary>
    [Test]
    public async Task TestSuicideByHeldItemSpreadDamage()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false
        });
        var server = pair.Server;
        var consoleHost = server.ResolveDependency<IConsoleHost>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        var handsSystem = entManager.System<SharedHandsSystem>();
        var mindSystem = entManager.System<SharedMindSystem>();
        var mobStateSystem = entManager.System<MobStateSystem>();
        var transformSystem = entManager.System<TransformSystem>();

        // We need to know the player and whether they can be hurt, killed, and whether they have a mind
        var player = playerMan.Sessions.First().AttachedEntity!.Value;
        var mind = mindSystem.GetMind(player);

        MindComponent mindComponent = default;
        MobStateComponent mobStateComp = default;
        MobThresholdsComponent mobThresholdsComp = default;
        DamageableComponent damageableComp = default;
        HandsComponent handsComponent = default;
        await server.WaitPost(() =>
        {
            if (mind != null)
                mindComponent = entManager.GetComponent<MindComponent>(mind.Value);

            mobStateComp = entManager.GetComponent<MobStateComponent>(player);
            mobThresholdsComp = entManager.GetComponent<MobThresholdsComponent>(player);
            damageableComp = entManager.GetComponent<DamageableComponent>(player);
            handsComponent = entManager.GetComponent<HandsComponent>(player);
        });

        // Spawn the weapon of choice and put it in the player's hands
        await server.WaitPost(() =>
        {
            var item = entManager.SpawnEntity("MixedDamageTestObject", transformSystem.GetMapCoordinates(player));
            Assert.That(handsSystem.TryPickup(player, item, handsComponent.ActiveHand!));
            entManager.TryGetComponent<ExecutionComponent>(item, out var executionComponent);
            Assert.That(executionComponent, Is.Not.EqualTo(null));
        });

        // Check that running the suicide command kills the player
        // and properly ghosts them without them being able to return to their body
        // and that slash damage is split in half
        await server.WaitAssertion(() =>
        {
            consoleHost.GetSessionShell(playerMan.Sessions.First()).ExecuteCommand("suicide");
            var lethalDamageThreshold = mobThresholdsComp.Thresholds.Keys.Last();

            Assert.Multiple(() =>
            {
                Assert.That(mobStateSystem.IsDead(player, mobStateComp));
                Assert.That(entManager.TryGetComponent<GhostComponent>(mindComponent.CurrentEntity, out var ghostComp) &&
                            !ghostComp.CanReturnToBody);
                Assert.That(damageableComp.Damage.DamageDict["Slash"], Is.EqualTo(lethalDamageThreshold / 2));
            });
        });

        await pair.CleanReturnAsync();
    }
}
