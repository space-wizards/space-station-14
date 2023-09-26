#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Climbing;
using Content.Shared.Climbing;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Climbing;

public sealed class ClimbingTest : MovementTest
{
    [Test]
    public async Task ClimbTableTest()
    {
        // Spawn a table to the right of the player.
        await SpawnTarget("Table");
        Assert.That(Delta(), Is.GreaterThan(0));

        // Player is not initially climbing anything.
        var comp = Comp<ClimbingComponent>(Player);
        Assert.Multiple(() =>
        {
            Assert.That(comp.IsClimbing, Is.False);
            Assert.That(comp.DisabledFixtureMasks, Has.Count.EqualTo(0));
        });

        // Attempt (and fail) to walk past the table.
        await Move(DirectionFlag.East, 1f);
        Assert.That(Delta(), Is.GreaterThan(0));

        // Try to start climbing
        var sys = SEntMan.System<ClimbSystem>();
        await Server.WaitPost(() => sys.TryClimb(SEntMan.GetEntity(Player), SEntMan.GetEntity(Player), SEntMan.GetEntity(Target.Value), out _));
        await AwaitDoAfters();

        // Player should now be climbing
        Assert.Multiple(() =>
        {
            Assert.That(comp.IsClimbing, Is.True);
            Assert.That(comp.DisabledFixtureMasks, Has.Count.GreaterThan(0));
        });

        // Can now walk over the table.
        await Move(DirectionFlag.East, 1f);

        Assert.Multiple(() =>
        {
            Assert.That(Delta(), Is.LessThan(0));

            // After walking away from the table, player should have stopped climbing.
            Assert.That(comp.IsClimbing, Is.False);
            Assert.That(comp.DisabledFixtureMasks, Has.Count.EqualTo(0));
        });

        // Try to walk back to the other side (and fail).
        await Move(DirectionFlag.West, 1f);
        Assert.That(Delta(), Is.LessThan(0));

        // Start climbing
        await Server.WaitPost(() => sys.TryClimb(SEntMan.GetEntity(Player), SEntMan.GetEntity(Player), SEntMan.GetEntity(Target.Value), out _));
        await AwaitDoAfters();

        Assert.Multiple(() =>
        {
            Assert.That(comp.IsClimbing, Is.True);
            Assert.That(comp.DisabledFixtureMasks, Has.Count.GreaterThan(0));
        });

        // Walk past table and stop climbing again.
        await Move(DirectionFlag.West, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(Delta(), Is.GreaterThan(0));
            Assert.That(comp.IsClimbing, Is.False);
            Assert.That(comp.DisabledFixtureMasks, Has.Count.EqualTo(0));
        });
    }
}
