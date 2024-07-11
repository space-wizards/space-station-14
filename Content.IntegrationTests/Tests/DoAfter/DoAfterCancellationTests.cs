using System.Linq;
using Content.IntegrationTests.Tests.Construction.Interaction;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Tests.Weldable;
using Content.Shared.Tools.Components;

namespace Content.IntegrationTests.Tests.DoAfter;

/// <summary>
/// This class has various tests that verify that cancelled DoAfters do not complete construction or other interactions.
/// It also checks that cancellation of a DoAfter does not block future DoAfters.
/// </summary>
public sealed class DoAfterCancellationTests : InteractionTest
{
    [Test]
    public async Task CancelWallDeconstruct()
    {
        await StartDeconstruction(WallConstruction.WallSolid);
        await InteractUsing(Weld, awaitDoAfters: false);

        // Failed do-after has no effect
        await CancelDoAfters();
        AssertPrototype(WallConstruction.WallSolid);

        // Second attempt works fine
        await InteractUsing(Weld);
        AssertPrototype(WallConstruction.Girder);

        // Repeat for wrenching interaction
        AssertAnchored();
        await InteractUsing(Wrench, awaitDoAfters: false);
        await CancelDoAfters();
        AssertAnchored();
        AssertPrototype(WallConstruction.Girder);
        await InteractUsing(Wrench);
        AssertAnchored(false);

        // Repeat for screwdriver interaction.
        AssertExists();
        await InteractUsing(Screw, awaitDoAfters: false);
        await CancelDoAfters();
        AssertExists();
        await InteractUsing(Screw);
        AssertDeleted();
    }

    [Test]
    public async Task CancelWallConstruct()
    {
        await StartConstruction(WallConstruction.Wall);
        await InteractUsing(Steel, 5, awaitDoAfters: false);
        await CancelDoAfters();

        await InteractUsing(Steel, 5);
        ClientAssertPrototype(WallConstruction.Girder, Target);
        await InteractUsing(Steel, 5, awaitDoAfters: false);
        await CancelDoAfters();
        AssertPrototype(WallConstruction.Girder);

        await InteractUsing(Steel, 5);
        AssertPrototype(WallConstruction.WallSolid);
    }

    [Test]
    public async Task CancelTilePry()
    {
        await SetTile(Floor);
        await InteractUsing(Pry, awaitDoAfters: false);
        await CancelDoAfters();
        await AssertTile(Floor);

        await InteractUsing(Pry);
        await AssertTile(Plating);
    }

    [Test]
    public async Task CancelRepeatedTilePry()
    {
        await SetTile(Floor);
        await InteractUsing(Pry, awaitDoAfters: false);
        await RunTicks(1);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
        await AssertTile(Floor);

        // Second DoAfter cancels the first.
        await Server.WaitPost(() => InteractSys.UserInteraction(SEntMan.GetEntity(Player), SEntMan.GetCoordinates(TargetCoords), SEntMan.GetEntity(Target)));
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        await AssertTile(Floor);

        // Third do after will work fine
        await InteractUsing(Pry);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        await AssertTile(Plating);
    }

    [Test]
    public async Task CancelRepeatedWeld()
    {
        await SpawnTarget(WeldableTests.Locker);
        var comp = Comp<WeldableComponent>();

        Assert.That(comp.IsWelded, Is.False);

        await InteractUsing(Weld, awaitDoAfters: false);
        await RunTicks(1);
        Assert.Multiple(() =>
        {
            Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
            Assert.That(comp.IsWelded, Is.False);
        });

        // Second DoAfter cancels the first.
        // Not using helper, because it runs too many ticks & causes the do-after to finish.
        await Server.WaitPost(() => InteractSys.UserInteraction(SEntMan.GetEntity(Player), SEntMan.GetCoordinates(TargetCoords), SEntMan.GetEntity(Target)));
        Assert.Multiple(() =>
        {
            Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
            Assert.That(comp.IsWelded, Is.False);
        });

        // Third do after will work fine
        await InteractUsing(Weld);
        Assert.Multiple(() =>
        {
            Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
            Assert.That(comp.IsWelded, Is.True);
        });

        // Repeat test for un-welding
        await InteractUsing(Weld, awaitDoAfters: false);
        await RunTicks(1);
        Assert.Multiple(() =>
        {
            Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
            Assert.That(comp.IsWelded, Is.True);
        });
        await Server.WaitPost(() => InteractSys.UserInteraction(SEntMan.GetEntity(Player), SEntMan.GetCoordinates(TargetCoords), SEntMan.GetEntity(Target)));
        Assert.Multiple(() =>
        {
            Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
            Assert.That(comp.IsWelded, Is.True);
        });
        await InteractUsing(Weld);
        Assert.Multiple(() =>
        {
            Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
            Assert.That(comp.IsWelded, Is.False);
        });
    }
}
