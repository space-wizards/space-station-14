using Content.IntegrationTests.Tests.Movement;
using Content.Server.NPC.HTN;
using Content.Shared.Damage.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mousetrap;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Mousetrap;

/// <summary>
/// Spawns a mouse and a mousetrap.
/// Makes the mouse cross the inactive mousetrap, then activates the trap and
/// makes the mouse try to cross back over it.
/// </summary>
/// <remarks>
/// Yep, every time the tests run, a virtual mouse dies. Sorry.
/// </remarks>
public sealed class MousetrapMouseMoveOverTest : MovementTest
{
    private static readonly EntProtoId MousetrapProtoId = "Mousetrap";
    private static readonly EntProtoId MouseProtoId = "MobMouse";
    protected override string PlayerPrototype => MouseProtoId.Id; // use a mouse as the player entity

    [Test]
    public async Task MouseMoveOverTest()
    {
        // Make sure the mouse doesn't have any AI active
        await Server.WaitPost(() => SEntMan.RemoveComponent<HTNComponent>(SPlayer));

        // Spawn a mouse trap
        await SpawnTarget(MousetrapProtoId);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Mouse and mousetrap not in expected positions.");

        Assert.That(HasComp<MousetrapComponent>(),
            $"{MousetrapProtoId} does not have a MousetrapComponent. If you're refactoring, please update this test!");

        Assert.That(TryComp<ItemToggleComponent>(out var itemToggleComp),
            $"{MousetrapProtoId} does not have a ItemToggleComponent. If you're refactoring, please update this test!");
        Assert.That(itemToggleComp.Activated, Is.False, "Mousetrap started active.");

        // The mouse is spawned by the test before the atmosphere is added, so it has some barotrauma damage already
        // TODO: fix this since it can have an impact on integration tests
        Assert.That(SEntMan.TryGetComponent<DamageableComponent>(SPlayer, out var damageComp),
            $"Player does not have a DamageableComponent.");
        var startingDamage = damageComp.TotalDamage;

        Assert.That(SEntMan.TryGetComponent<MobStateComponent>(SPlayer, out var mouseMobStateComp),
            $"{MouseProtoId} does not have a MobStateComponent.");
        Assert.That(mouseMobStateComp.CurrentState, Is.EqualTo(MobState.Alive), "Mouse was not alive when spawned.");

        // Move mouse over the trap
        await Move(DirectionFlag.East, 1f);

        Assert.That(Delta(), Is.LessThan(0.5), "Mouse did not move over mousetrap.");

        // Walking over an inactive trap does not trigger it
        Assert.That(damageComp.TotalDamage, Is.LessThanOrEqualTo(startingDamage), "Mouse took damage from inactive trap!");
        Assert.That(itemToggleComp.Activated, Is.False, "Mousetrap was activated.");

        // Activate the trap
        var itemToggleSystem = Server.System<ItemToggleSystem>();
        await Server.WaitAssertion(() =>
        {
            Assert.That(itemToggleSystem.TrySetActive(STarget.Value, true), "Could not activate the mouse trap.");
        });

        await Move(DirectionFlag.West, 1f);
        Assert.That(Delta(), Is.LessThan(0.1), "Mouse moved past active mousetrap.");

        // Walking over an active trap triggers it
        Assert.That(damageComp.TotalDamage, Is.GreaterThan(startingDamage), "Mouse did not take damage from active trap!");
        Assert.That(itemToggleComp.Activated, Is.False, "Mousetrap was not deactivated after triggering.");
        Assert.That(mouseMobStateComp.CurrentState, Is.EqualTo(MobState.Dead), "Mouse was not killed by trap.");
    }
}

/// <summary>
/// Spawns a mousetrap and makes the player walk over it without shoes.
/// Gives the player some shoes and makes them walk back over the trap.
/// </summary>
public sealed class MousetrapHumanMoveOverTest : MovementTest
{
    private static readonly EntProtoId MousetrapProtoId = "Mousetrap";
    private const string ShoesProtoId = "InteractionTestShoes";

    [TestPrototypes]
    private static readonly string TestPrototypes = $@"
    - type: entity
      parent: ClothingShoesBase
      id: {ShoesProtoId}
      components:
      - type: Sprite
        sprite: Clothing/Shoes/Boots/workboots.rsi
    ";

    [Test]
    public async Task HumanMoveOverTest()
    {
        await SpawnTarget(MousetrapProtoId);

        Assert.That(Delta(), Is.GreaterThan(0.5), "Player and mousetrap not in expected positions.");

        Assert.That(HasComp<MousetrapComponent>(),
            $"{MousetrapProtoId} does not have a MousetrapComponent. If you're refactoring, please update this test!");

        Assert.That(TryComp<ItemToggleComponent>(out var itemToggleComp),
            $"{MousetrapProtoId} does not have a ItemToggleComponent. If you're refactoring, please update this test!");

        // Activate the trap
        var itemToggleSystem = Server.System<ItemToggleSystem>();
        await Server.WaitAssertion(() =>
        {
            Assert.That(itemToggleSystem.TrySetActive(STarget.Value, true), "Could not activate the mouse trap.");
        });

        Assert.That(SEntMan.TryGetComponent<DamageableComponent>(SPlayer, out var damageComp),
            $"Player does not have a DamageableComponent.");
        var startingDamage = damageComp.TotalDamage;

        // Move player over the trap
        await Move(DirectionFlag.East, 0.5f);

        Assert.That(Delta(), Is.LessThan(0.5), "Player did not move over mousetrap.");

        // Walking over the trap without shoes activates it
        Assert.That(damageComp.TotalDamage, Is.GreaterThan(startingDamage), "Player did not take damage.");
        Assert.That(itemToggleComp.Activated, Is.False, "Mousetrap was not deactivated after triggering.");

        // Reactivate the trap
        await Server.WaitAssertion(() =>
        {
            Assert.That(itemToggleSystem.TrySetActive(STarget.Value, true), "Could not activate the mouse trap.");
        });
        var afterStepDamage = damageComp.TotalDamage;

        // Give the player some shoes
        await PlaceInHands(ShoesProtoId);
        // Thanks to quick-equip, using the shoes will wear them
        await UseInHand();

        // Move back over the trap
        await Move(DirectionFlag.West, 1f);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not move back over mousetrap.");

        // Walking over the trap with shoes on does not activate it
        Assert.That(damageComp.TotalDamage, Is.LessThanOrEqualTo(afterStepDamage), "Player took damage from trap!");
        Assert.That(itemToggleComp.Activated, "Mousetrap was deactivated despite the player being protected by shoes.");
    }
}
