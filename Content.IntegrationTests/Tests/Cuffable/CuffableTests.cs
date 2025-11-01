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
    /// Cuff and uncuff ourselves
    /// </summary>
    [Test]
    public async Task TestSelfCuffingAndUncuffing()
    {
        var blockerSystem = Server.System<ActionBlockerSystem>();
        var sys = Server.System<SharedCuffableSystem>();

        // Find our(cuffable)selves
        var comp = SEntMan.GetComponent<CuffableComponent>(SPlayer);
        var xform = SEntMan.GetComponent<TransformComponent>(SPlayer);
        var ent = (SPlayer, comp);

        // Place a pair of cuffs in our hands
        var cuffs = await PlaceInHands(Handcuffs);
        var sCuffs = ToServer(cuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicks(5);

        // Make sure we're not already cuffed!
        await AssertUncuffed(sys, blockerSystem, ent);

        // Make sure we're holding the cuffs
        Assert.That(HandSys.EnumerateHeld(SPlayer).Contains(sCuffs));

        // Use the cuffs in our hands on ourselves
        await Interact(SPlayer, xform.Coordinates);
        await RunTicks(5);

        await AssertCuffed(sys, blockerSystem, ent, sCuffs);

        // Make sure we're not holding either pair of cuffs!
        Assert.That(!HandSys.EnumerateHeld(SPlayer).Contains(sCuffs));

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(sys.GetAllCuffs(ent).Count == 1);

        // Try to uncuff ourselves!
        await Server.WaitPost(() =>
        {
            sys.TryUncuff(ent, SPlayer);
        });
        await RunSeconds(20);

        // Check that we're no longer cuffed!
        await AssertUncuffed(sys, blockerSystem, ent);
    }

    /// <summary>
    /// Cuff and uncuff oa target
    /// </summary>
    [Test]
    public async Task TestTargetCuffingAndUncuffing()
    {
        var blockerSystem = Server.System<ActionBlockerSystem>();
        var sys = Server.System<SharedCuffableSystem>();
        await SpawnTarget(Traitor);

        // Get cuffable target
        var comp = SEntMan.GetComponent<CuffableComponent>(STarget.Value);
        var ent = (STarget.Value, comp);

        // Place a pair of cuffs in our hands
        var cuffs = await PlaceInHands(Handcuffs);
        var sCuffs = ToServer(cuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicks(5);

        // Ensure the target isn't already cuffed!
        await AssertUncuffed(sys, blockerSystem, ent);

        // Make sure we're holding the cuffs!
        Assert.That(HandSys.EnumerateHeld(SPlayer).Contains(sCuffs));

        // Use the cuffs in our hands on the target
        await Interact();
        await RunTicks(5);

        await AssertCuffed(sys, blockerSystem, ent, sCuffs);

        // Make sure we're not holding either pair of cuffs!
        Assert.That(!HandSys.EnumerateHeld(SPlayer).Contains(sCuffs));

        // Make sure the target isn't holding the cuffs somehow either!
        Assert.That(!HandSys.EnumerateHeld(STarget.Value).Contains(sCuffs));

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(sys.GetAllCuffs(STarget.Value).Count == 1);

        // Try to uncuff the target!
        await Server.WaitPost(() =>
        {
            sys.TryUncuff(ent, SPlayer);
        });
        await RunSeconds(20);

        // Assert the target is no longer cuffed!
        await AssertUncuffed(sys, blockerSystem, ent);
    }

    /// <summary>
    /// Cuff a target and remove their hands
    /// </summary>
    [Test]
    public async Task TestCuffingHandLoss()
    {
        var blockerSystem = Server.System<ActionBlockerSystem>();
        var sys = Server.System<SharedCuffableSystem>();
        await SpawnTarget(Traitor);

        // Get cuffable entity
        var comp = SEntMan.GetComponent<CuffableComponent>(STarget.Value);
        var hands = SEntMan.GetComponent<HandsComponent>(STarget.Value);
        var ent = (STarget.Value, comp);

        // Place a pair of cuffs in our hands
        var cuffs = await PlaceInHands(Handcuffs);
        var sCuffs = ToServer(cuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicks(5);

        await AssertUncuffed(sys, blockerSystem, ent);

        // Make sure we're holding the cuffs!
        Assert.That(HandSys.EnumerateHeld(SPlayer).Contains(sCuffs));

        // Cuff our target
        await Interact();
        await RunTicks(5);

        await AssertCuffed(sys, blockerSystem, ent, sCuffs);

        // Make sure we're not holding the cuffs!
        Assert.That(!HandSys.EnumerateHeld(SPlayer).Contains(sCuffs));

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(sys.GetAllCuffs(ent).Count == 1);

        // Remove one hand from the target!
        await Server.WaitPost(() =>
        {
            HandSys.RemoveHand(STarget.Value, hands.ActiveHandId);
        });

        // Make sure they're still cuffed! (They should have two hands)
        await AssertCuffed(sys, blockerSystem, ent, sCuffs);

        // Remove the last hand from the target!
        await Server.WaitPost(() =>
        {
            HandSys.RemoveHand(STarget.Value, hands.ActiveHandId);
        });

        await AssertUncuffed(sys, blockerSystem, ent);
    }

    public async Task AssertCannotInteract(ActionBlockerSystem blockerSystem, EntityUid player)
    {
        await Assert.MultipleAsync(() =>
        {
            // Make sure we can't attack!
            Assert.That(!blockerSystem.CanAttack(player));

            // Make sure we can't interact!
            Assert.That(!blockerSystem.CanInteract(player, null));

            return Task.CompletedTask;
        });
    }

    public async Task AssertCuffed(SharedCuffableSystem sys, ActionBlockerSystem blockerSystem, Entity<CuffableComponent> target, EntityUid cuffs)
    {
        await Assert.MultipleAsync(() =>
        {
            // Check that we're now cuffed!
            Assert.That(sys.IsCuffed(target));

            // Make sure that our list of all cuffs has the cuffs
            Assert.That(sys.GetAllCuffs(target), Has.Member(cuffs));

            // Make sure last added pair of cuffs is the cuff we're looking for!
            Assert.That(sys.GetLastCuffOrNull(target), Is.EqualTo(cuffs));

            return Task.CompletedTask;
        });

        await AssertCannotInteract(blockerSystem, target);
    }

    public async Task AssertUncuffed(SharedCuffableSystem sys, ActionBlockerSystem blockerSystem, Entity<CuffableComponent> target)
    {
        await Assert.MultipleAsync(() =>
        {
            // Check that we're not cuffed!
            Assert.That(!sys.IsCuffed(target));

            // Make sure that our list of all cuffs is empty!
            Assert.That(sys.GetAllCuffs(target), Is.Empty);

            // Make sure last added pair of cuffs is null
            Assert.That(sys.GetLastCuffOrNull(target), Is.Null);

            // Make sure we can attack!
            Assert.That(blockerSystem.CanAttack(target));

            // Make sure we can interact!
            Assert.That(blockerSystem.CanInteract(target, null));

            return Task.CompletedTask;
        });
    }
}
