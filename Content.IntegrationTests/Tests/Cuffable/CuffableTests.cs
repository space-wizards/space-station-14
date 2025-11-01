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
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var blockerSystem = server.System<ActionBlockerSystem>();
        var handsSys = entMan.System<SharedHandsSystem>();
        var sys = server.System<SharedCuffableSystem>();

        await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        // Acquire the handcuffs
        EntityUid player = default!;
        CuffableComponent comp = default!;
        await server.WaitPost(() =>
        {
            player = playerMan.Sessions.First().AttachedEntity!.Value;
            comp = entMan.GetComponent<CuffableComponent>(player);
        });

        var ent = (player, comp);

        // Place a pair of cuffs in our hands
        var netCuffs = await PlaceInHands("Handcuffs");
        var cuffs = entMan.GetEntity(netCuffs);

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await pair.RunTicksSync(5);

        // Make sure we're not already cuffed!
        Assert.That(!sys.IsCuffed(ent));

        // Make sure we have no cuffs
        Assert.That(sys.GetAllCuffs(ent).Count == 0);

        // Make sure we're holding both cuffs
        Assert.That(handsSys.EnumerateHeld(player).Contains(cuffs));

        // Use the cuffs in our hands on ourselves
        await Interact();
        await pair.RunTicksSync(5);

        await AssertCuffed(sys, ent, cuffs);

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.EqualTo(cuffs));

        // Make sure we're not holding either pair of cuffs!
        Assert.That(!handsSys.EnumerateHeld(player).Contains(cuffs));

        // Make sure it's the only pair of cuffs in that list!
        Assert.That(sys.GetAllCuffs(player).Count == 1);

        await AssertCannotInteract(blockerSystem, player);

        await InteractUsing("Handcuffs");
        await pair.RunTicksSync(5);

        await AssertCuffed(sys, ent, cuffs);

        // Make sure we have two pairs of cuffs now!
        Assert.That(sys.GetAllCuffs(player).Count == 2);

        await AssertCannotInteract(blockerSystem, player);

        // Try to uncuff ourselves!
        await server.WaitPost(() =>
        {
            sys.TryUncuff(player, player);
        });
        await pair.RunSeconds(10);

        // Check that we're still cuffed!
        await AssertCuffed(sys, ent, cuffs);

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.EqualTo(cuffs));

        // Make sure we only have one pair of cuffs now!
        Assert.That(sys.GetAllCuffs(player).Count == 1);

        // Try to uncuff ourselves!
        await server.WaitPost(() =>
        {
            sys.TryUncuff(player, player);
        });
        await pair.RunSeconds(10);

        // Check that we're now cuffed!
        Assert.That(!sys.IsCuffed(player));

        // Make sure the last pair of cuffs added is the one we expect
        Assert.That(sys.GetLastCuffOrNull(player), Is.Null);

        // Make sure we only have one pair of cuffs now!
        Assert.That(sys.GetAllCuffs(player).Count == 0);
    }

    [Test]
    public async Task TestCuffingHandLoss()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        var entMan = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mapSystem = server.System<SharedMapSystem>();
        var sys = entMan.System<SharedHandsSystem>();
        var tSys = entMan.System<TransformSystem>();
        var containerSystem = server.System<SharedContainerSystem>();
        var cuffableSystem = server.System<SharedCuffableSystem>();
    }

    public async Task AssertCannotInteract(ActionBlockerSystem blockerSystem, EntityUid player)
    {
        await Assert.MultipleAsync(() =>
        {
            // Make sure we can't attack!
            Assert.That(!blockerSystem.CanAttack(player));

            // Make sure we can't interact!
            Assert.That(!blockerSystem.CanInteract(player, null));

            // Make sure we can't complex interact!
            Assert.That(!blockerSystem.CanComplexInteract(player));

            // Assert we can still move!
            Assert.That(blockerSystem.CanMove(player));

            // Assert we can still emote!
            Assert.That(blockerSystem.CanEmote(player));

            // Assert we can still speak!
            Assert.That(blockerSystem.CanSpeak(player));

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
