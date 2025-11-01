using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Cuffable;

/// <summary>
/// This handles...
/// </summary>
public sealed class CuffableTests : InteractionTest
{
    [Test]
    public async Task TestCuffingAndUncuffing()
    {
        await Setup();

        var blockerSystem = Server.System<ActionBlockerSystem>();
        var sys = Server.System<SharedCuffableSystem>();

        // Acquire the handcuffs
        EntityUid player = default!;
        CuffableComponent comp = default!;
        TransformComponent xform = default!;
        await Server.WaitPost(() =>
        {
            player = SEntMan.GetEntity(Player);
            comp = SEntMan.GetComponent<CuffableComponent>(player);
            xform = SEntMan.GetComponent<TransformComponent>(player);
        });

        var ent = (player, comp);

        // Place a pair of cuffs in our hands
        var netCuffs = await PlaceInHands("Handcuffs");
        var cuffs = SEntMan.GetEntity(netCuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await Pair.RunTicksSync(5);

        // Make sure we're not already cuffed!
        Assert.That(!sys.IsCuffed(ent));

        // Make sure we have no cuffs
        Assert.That(sys.GetAllCuffs(ent).Count == 0);

        // Make sure we're holding both cuffs
        Assert.That(HandSys.EnumerateHeld(player).Contains(cuffs));

        // Use the cuffs in our hands on ourselves
        await Interact(player, xform.Coordinates);
        await Pair.RunTicksSync(5);

        await AssertCuffed(sys, ent, cuffs);

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.EqualTo(cuffs));

        // Make sure we're not holding either pair of cuffs!
        Assert.That(!HandSys.EnumerateHeld(player).Contains(cuffs));

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(sys.GetAllCuffs(player).Count == 1);

        await AssertCannotInteract(blockerSystem, player);

        // Try to uncuff ourselves!
        await Server.WaitPost(() =>
        {
            sys.TryUncuff(player, player);
        });
        await Pair.RunSeconds(20);

        // Check that we're no longer cuffed!
        Assert.That(!sys.IsCuffed(ent));

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.Null);

        // Make sure we only have one pair of cuffs now!
        Assert.That(sys.GetAllCuffs(player).Count == 0);

        // Make sure we can't attack!
        Assert.That(blockerSystem.CanAttack(player));

        // Make sure we can't interact!
        Assert.That(blockerSystem.CanInteract(player, null));
    }

    [Test]
    public async Task TestCuffingHandLoss()
    {
        await Setup();

        var blockerSystem = Server.System<ActionBlockerSystem>();
        var sys = Server.System<SharedCuffableSystem>();

        // Acquire the handcuffs
        EntityUid player = default!;
        CuffableComponent comp = default!;
        TransformComponent xform = default!;
        await Server.WaitPost(() =>
        {
            player = SEntMan.GetEntity(Player);
            comp = SEntMan.GetComponent<CuffableComponent>(player);
            xform = SEntMan.GetComponent<TransformComponent>(player);
        });

        var ent = (player, comp);

        // Place a pair of cuffs in our hands
        var netCuffs = await PlaceInHands("Handcuffs");
        var cuffs = SEntMan.GetEntity(netCuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await Pair.RunTicksSync(5);

        // Make sure we're not already cuffed!
        Assert.That(!sys.IsCuffed(ent));

        // Make sure we have no cuffs
        Assert.That(sys.GetAllCuffs(ent).Count == 0);

        // Make sure we're holding both cuffs
        Assert.That(HandSys.EnumerateHeld(player).Contains(cuffs));

        // Use the cuffs in our hands on ourselves
        await Interact(player, xform.Coordinates);
        await Pair.RunTicksSync(5);

        await AssertCuffed(sys, ent, cuffs);

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.EqualTo(cuffs));

        // Make sure we're not holding either pair of cuffs!
        Assert.That(!HandSys.EnumerateHeld(player).Contains(cuffs));

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(sys.GetAllCuffs(player).Count == 1);

        await AssertCannotInteract(blockerSystem, player);

        // Remove our only hand!
        await Server.WaitPost(() =>
        {
            HandSys.RemoveHand(player, Hands.ActiveHandId);
        });

        // Check that we're no longer cuffed!
        Assert.That(!sys.IsCuffed(ent));

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.Null);

        // Make sure we only have one pair of cuffs now!
        Assert.That(sys.GetAllCuffs(player).Count == 0);

        // Make sure we can't attack!
        Assert.That(blockerSystem.CanAttack(player));

        // Make sure we can't interact!
        Assert.That(blockerSystem.CanInteract(player, null));
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

    public async Task AssertCuffed(SharedCuffableSystem sys, Entity<CuffableComponent> player, EntityUid cuffs)
    {
        await Assert.MultipleAsync(() =>
        {
            // Check that we're now cuffed!
            Assert.That(sys.IsCuffed(player));

            // Make sure that our list of all cuffs has the cuffs
            Assert.That(sys.GetAllCuffs(player), Has.Member(cuffs));

            return Task.CompletedTask;
        });
    }
}
