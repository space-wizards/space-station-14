using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Cuffable;

/// <summary>
/// This tests the cuffable system to ensure that all aspects of it are functional.
/// </summary>
public sealed class CuffableTests : InteractionTest
{
    private static readonly EntProtoId Handcuffs = "Handcuffs";
    private static readonly EntProtoId Traitor = "MobHuman";

    /// <summary>
    /// Cuff and uncuff ourselves.
    /// </summary>
    [Test]
    public async Task TestSelfCuffingAndUncuffing()
    {
        var blockerSystem = Server.System<ActionBlockerSystem>();
        var cuffableSystem = Server.System<SharedCuffableSystem>();

        // Find our(cuffable)selves
        var cuffable = Comp<CuffableComponent>(Player);
        var ent = (SPlayer, cuffable);

        // Place a pair of cuffs in our hands
        var cuffs = await PlaceInHands(Handcuffs);
        var sCuffs = ToServer(cuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicks(5);

        // Make sure we're not already cuffed!
        await AssertUncuffed(cuffableSystem, blockerSystem, ent);

        // Make sure we're holding the cuffs
        Assert.That(HandSys.IsHolding(SPlayer, sCuffs), "Player was not holding the handcuffs.");

        // Use the cuffs in our hands on ourselves
        await Interact(SPlayer, Position(SPlayer));
        await RunTicks(5);

        await AssertCuffed(cuffableSystem, blockerSystem, ent, sCuffs);

        // Make sure we're not holding the cuffs!
        Assert.That(HandSys.IsHolding(SPlayer, sCuffs), Is.False, "Player was still holding the handcuffs after cuffing themselves.");

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(cuffableSystem.GetAllCuffs(ent), Has.Count.EqualTo(1), "Player was cuffed with more than one handcuff.");

        // Try to uncuff ourselves!
        await Server.WaitPost(() =>
        {
            cuffableSystem.TryUncuff(ent, SPlayer);
        });
        await RunSeconds(20);

        // Check that we're no longer cuffed!
        await AssertUncuffed(cuffableSystem, blockerSystem, ent);
    }

    /// <summary>
    /// Cuff and uncuff a target.
    /// </summary>
    [Test]
    public async Task TestTargetCuffingAndUncuffing()
    {
        var blockerSystem = Server.System<ActionBlockerSystem>();
        var cuffableSystem = Server.System<SharedCuffableSystem>();
        await SpawnTarget(Traitor);

        // Get cuffable target
        var cuffable = Comp<CuffableComponent>(Target.Value);
        var ent = (STarget.Value, cuffable);

        // Place a pair of cuffs in our hands
        var cuffs = await PlaceInHands(Handcuffs);
        var sCuffs = ToServer(cuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicks(5);

        // Ensure the target isn't already cuffed!
        await AssertUncuffed(cuffableSystem, blockerSystem, ent);

        // Make sure we're holding the cuffs!
        Assert.That(HandSys.IsHolding(SPlayer, sCuffs), "Player was not holding the handcuffs.");

        // Use the cuffs in our hands on the target
        await Interact();
        await RunTicks(5);

        await AssertCuffed(cuffableSystem, blockerSystem, ent, sCuffs);

        // Make sure we're not holding the cuffs!
        Assert.That(HandSys.IsHolding(SPlayer, sCuffs), Is.False, "Player was still holding the handcuffs after cuffing the target.");

        // Make sure the target isn't holding the cuffs somehow either!
        Assert.That(HandSys.IsHolding(STarget.Value, sCuffs), Is.False, "Target was holding the handcuffs after being cuffed.");

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(cuffableSystem.GetAllCuffs(ent), Has.Count.EqualTo(1), "Target was cuffed with more than one handcuff.");

        // Try to uncuff the target!
        await Server.WaitPost(() =>
        {
            cuffableSystem.TryUncuff(ent, SPlayer);
        });
        await RunSeconds(20);

        // Assert the target is no longer cuffed!
        await AssertUncuffed(cuffableSystem, blockerSystem, ent);
    }

    /// <summary>
    /// Cuff a target and remove their hands.
    /// </summary>
    [Test]
    public async Task TestCuffingHandLoss()
    {
        var blockerSystem = Server.System<ActionBlockerSystem>();
        var cuffableSystem = Server.System<SharedCuffableSystem>();
        await SpawnTarget(Traitor);

        // Get cuffable entity
        var cuffable = Comp<CuffableComponent>(Target.Value);
        var hands = Comp<HandsComponent>(Target.Value);
        var ent = (STarget.Value, cuffable);

        // Place a pair of cuffs in our hands
        var cuffs = await PlaceInHands(Handcuffs);
        var sCuffs = ToServer(cuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicks(5);

        await AssertUncuffed(cuffableSystem, blockerSystem, ent);

        // Make sure we're holding the cuffs
        Assert.That(HandSys.IsHolding(SPlayer, sCuffs), "Player was not holding the handcuffs.");

        // Cuff our target
        await Interact();
        await RunTicks(5);

        await AssertCuffed(cuffableSystem, blockerSystem, ent, sCuffs);

        // Make sure we're not holding the cuffs!
        Assert.That(HandSys.IsHolding(SPlayer, sCuffs), Is.False, "Player was still holding the handcuffs after cuffing the target.");

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(cuffableSystem.GetAllCuffs(ent), Has.Count.EqualTo(1), "Target was cuffed with more than one handcuff.");

        // Remove one hand from the target!
        await Server.WaitPost(() =>
        {
            HandSys.RemoveHand(STarget.Value, hands.ActiveHandId);
        });

        // Make sure they're still cuffed! (They should have two hands)
        await AssertCuffed(cuffableSystem, blockerSystem, ent, sCuffs);

        // Remove the last hand from the target!
        await Server.WaitPost(() =>
        {
            HandSys.RemoveHand(STarget.Value, hands.ActiveHandId);
        });

        await AssertUncuffed(cuffableSystem, blockerSystem, ent);
    }

    public async Task AssertCuffed(SharedCuffableSystem sys, ActionBlockerSystem blockerSystem, Entity<CuffableComponent> target, EntityUid cuffs)
    {
        await Assert.MultipleAsync(() =>
        {
            // Check that we're now cuffed!
            Assert.That(sys.IsCuffed(target), "Target was not cuffed.");

            // Make sure that our list of all cuffs has the cuffs
            Assert.That(sys.GetAllCuffs(target), Has.Member(cuffs), "The used cuffs were not in the handcuff container.");

            // Make sure last added pair of cuffs is the cuff we're looking for!
            Assert.That(sys.GetLastCuffOrNull(target), Is.EqualTo(cuffs), "The last cuffs were not set correctly.");

            // Make sure we can't attack!
            Assert.That(blockerSystem.CanAttack(target), Is.False, "Cuffed mob is still able to attack.");

            // Make sure we can't interact!
            Assert.That(blockerSystem.CanInteract(target, null), Is.False, "Cuffed mob is still able to interact.");

            return Task.CompletedTask;
        });

    }

    public async Task AssertUncuffed(SharedCuffableSystem sys, ActionBlockerSystem blockerSystem, Entity<CuffableComponent> target)
    {
        await Assert.MultipleAsync(() =>
        {
            // Check that we're not cuffed!
            Assert.That(sys.IsCuffed(target), Is.False, "Target was cuffed.");

            // Make sure that our list of all cuffs is empty!
            Assert.That(sys.GetAllCuffs(target), Is.Empty, "Hnadcuff container was not empty.");

            // Make sure last added pair of cuffs is null
            Assert.That(sys.GetLastCuffOrNull(target), Is.Null, "Last added cuffs were not null.");

            // Make sure we can attack!
            Assert.That(blockerSystem.CanAttack(target), "Uncuffed mob is not able to attack.");

            // Make sure we can interact!
            Assert.That(blockerSystem.CanInteract(target, null), "Uncuffed mob is not able to interact.");

            return Task.CompletedTask;
        });
    }
}
