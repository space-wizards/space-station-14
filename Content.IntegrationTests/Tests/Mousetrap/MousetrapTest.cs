using Content.IntegrationTests.Tests.Movement;
using Content.Server.NPC.HTN;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mousetrap;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Mousetrap;

public sealed class MousetrapTest : MovementTest
{
    private static readonly EntProtoId MousetrapProtoId = "Mousetrap";
    private static readonly EntProtoId MouseProtoId = "MobMouse";

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

    /// <summary>
    /// Spawns a mousetrap, then makes the player pick it up and toggle it on and off.
    /// </summary>
    [Test]
    public async Task PlayerToggleOnOffTest()
    {
        await SpawnTarget(MousetrapProtoId);

        Assert.That(TryComp<MousetrapComponent>(out var mousetrapComp),
            $"{MousetrapProtoId} does not have a MousetrapComponent. If you're refactoring, please update this test!");
        Assert.That(mousetrapComp.IsActive, Is.False, "Mousetrap spawned active.");

        await Pickup();

        Assert.That(mousetrapComp.IsActive, Is.False, "Picking up mousetrap activated it.");

        await UseInHand();

        Assert.That(mousetrapComp.IsActive, "Mousetrap was not activated by UseInHand.");

        await UseInHand();

        Assert.That(mousetrapComp.IsActive, Is.False, "Mousetrap was not deactivated by UseInHand.");
    }

    /// <summary>
    /// Spawns a mouse and a mousetrap. Transfers the player's control to the mouse.
    /// Makes the mouse cross the inactive mousetrap, then activates the trap and
    /// makes the mouse try to cross back over it.
    /// </summary>
    /// <remarks>
    /// Yep, every time the tests run, a virtual mouse dies. Sorry.
    /// </remarks>
    [Test]
    public async Task MouseMoveOverTest()
    {
        await SpawnEntity(MouseProtoId.Id, SEntMan.GetCoordinates(PlayerCoords));
        await SpawnTarget(MousetrapProtoId);
        var mouseEnt = await FindEntity(MouseProtoId.Id);

        // Make sure the mouse doesn't have any AI active
        await Server.WaitPost(() => SEntMan.RemoveComponent<HTNComponent>(mouseEnt));

        // Transfer player control to the mouse
        var mindSys = SEntMan.System<SharedMindSystem>();
        await Server.WaitPost(() => mindSys.ControlMob(SEntMan.GetEntity(Player), mouseEnt));
        // Why not use the PlayerPrototype override? Because it errors on anything without hands and I don't feel like fixing that right now.
        Player = SEntMan.GetNetEntity(mouseEnt);
        // Player should now refer to the mouse
        AssertPrototype(MouseProtoId, Player);

        Assert.That(Delta(), Is.GreaterThan(0.5), "Mouse and mousetrap not in expected positions.");

        Assert.That(TryComp<MousetrapComponent>(out var mousetrapComp),
            $"{MousetrapProtoId} does not have a MousetrapComponent. If you're refactoring, please update this test!");
        Assert.That(mousetrapComp.IsActive, Is.False, "Mousetrap started active.");

        Assert.That(SEntMan.TryGetComponent<DamageableComponent>(SEntMan.GetEntity(Player), out var mouseDamageComp),
            $"{MouseProtoId} does not have a DamageableComponent.");
        Assert.That(mouseDamageComp.TotalDamage, Is.EqualTo(FixedPoint2.Zero));

        Assert.That(SEntMan.TryGetComponent<MobStateComponent>(SEntMan.GetEntity(Player), out var mouseMobStateComp),
            $"{MouseProtoId} does not have a MobStateComponent.");
        Assert.That(mouseMobStateComp.CurrentState, Is.EqualTo(MobState.Alive), "Mouse was dead to begin with.");

        // Move mouse over the trap
        await Move(DirectionFlag.East, 1f);

        Assert.That(Delta(), Is.LessThan(0.5), "Mouse did not move over mousetrap.");

        // Walking over an inactive trap does not trigger it
        Assert.That(mouseDamageComp.TotalDamage, Is.EqualTo(FixedPoint2.Zero), "Mouse took damage from inactive trap!");
        Assert.That(mousetrapComp.IsActive, Is.False, "Mousetrap was activated.");

        // Activate the trap
        mousetrapComp.IsActive = true;

        await Move(DirectionFlag.West, 1f);
        Assert.That(Delta(), Is.LessThan(0.1), "Mouse moved past active mousetrap.");

        // Walking over an active trap triggers it
        Assert.That(mouseDamageComp.TotalDamage, Is.GreaterThan(FixedPoint2.Zero), "Mouse did not take damage from active trap!");
        Assert.That(mousetrapComp.IsActive, Is.False, "Mousetrap was not deactivated after triggering.");
        Assert.That(mouseMobStateComp.CurrentState, Is.EqualTo(MobState.Dead), "Mouse was not killed by trap.");
    }

    /// <summary>
    /// Spawns a mousetrap and makes the player walk over it without shoes.
    /// Gives the player some shoes and makes them walk back over the trap.
    /// </summary>
    [Test]
    public async Task HumanMoveOverTest()
    {
        await SpawnTarget(MousetrapProtoId);

        Assert.That(Delta(), Is.GreaterThan(0.5), "Player and mousetrap not in expected positions.");

        Assert.That(TryComp<MousetrapComponent>(out var mousetrapComp),
            $"{MousetrapProtoId} does not have a MousetrapComponent. If you're refactoring, please update this test!");
        // Activate the trap
        mousetrapComp.IsActive = true;

        Assert.That(SEntMan.TryGetComponent<DamageableComponent>(SEntMan.GetEntity(Player), out var damageComp),
            $"Player does not have a DamageableComponent.");
        var startingDamage = damageComp.TotalDamage;

        // Move player over the trap
        await Move(DirectionFlag.East, 0.5f);

        Assert.That(Delta(), Is.LessThan(0.5), "Player did not move over mousetrap.");

        // Walking over the trap without shoes activates it
        Assert.That(damageComp.TotalDamage, Is.GreaterThan(startingDamage), "Player did not take damage.");
        Assert.That(mousetrapComp.IsActive, Is.False, "Mousetrap was not deactivated after triggering.");

        // Reactivate the trap
        mousetrapComp.IsActive = true;
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
        Assert.That(mousetrapComp.IsActive, "Mousetrap was deactivated.");
    }
}
