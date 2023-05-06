using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Construction.Interaction;
using Content.IntegrationTests.Tests.Interaction;
using Content.IntegrationTests.Tests.Weldable;
using Content.Server.Tools.Components;
using NUnit.Framework;

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
        await Interact(Weld, awaitDoAfters:false);

        // Failed do-after has no effect
        await CancelDoAfters();
        AssertPrototype(WallConstruction.WallSolid);

        // Second attempt works fine
        await Interact(Weld);
        AssertPrototype(WallConstruction.Girder);

        // Repeat for wrenching interaction
        AssertAnchored();
        await Interact(Wrench, awaitDoAfters:false);
        await CancelDoAfters();
        AssertAnchored();
        AssertPrototype(WallConstruction.Girder);
        await Interact(Wrench);
        AssertAnchored(false);

        // Repeat for screwdriver interaction.
        AssertDeleted(false);
        await Interact(Screw, awaitDoAfters:false);
        await CancelDoAfters();
        AssertDeleted(false);
        await Interact(Screw);
        AssertDeleted();
    }

    [Test]
    public async Task CancelWallConstruct()
    {
        await StartConstruction(WallConstruction.Wall);
        await Interact(Steel, 5, awaitDoAfters:false);
        await CancelDoAfters();
        Assert.That(Target.HasValue && Target.Value.IsClientSide());

        await Interact(Steel, 5);
        AssertPrototype(WallConstruction.Girder);
        await Interact(Steel, 5, awaitDoAfters:false);
        await CancelDoAfters();
        AssertPrototype(WallConstruction.Girder);

        await Interact(Steel, 5);
        AssertPrototype(WallConstruction.WallSolid);
    }

    [Test]
    public async Task CancelTilePry()
    {
        await SetTile(Floor);
        await Interact(Pry, awaitDoAfters:false);
        await CancelDoAfters();
        await AssertTile(Floor);

        await Interact(Pry);
        await AssertTile(Plating);
    }

    [Test]
    public async Task CancelRepeatedTilePry()
    {
        await SetTile(Floor);
        await Interact(Pry, awaitDoAfters:false);
        await RunTicks(1);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
        await AssertTile(Floor);

        // Second DoAfter cancels the first.
        await Server.WaitPost(() => InteractSys.UserInteraction(Player, TargetCoords, Target));
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        await AssertTile(Floor);

        // Third do after will work fine
        await Interact(Pry);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        await AssertTile(Plating);
    }

    [Test]
    public async Task CancelRepeatedWeld()
    {
        await SpawnTarget(WeldableTests.Locker);
        var comp = Comp<WeldableComponent>();

        Assert.That(comp.Weldable, Is.True);
        Assert.That(comp.IsWelded, Is.False);

        await Interact(Weld, awaitDoAfters:false);
        await RunTicks(1);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
        Assert.That(comp.IsWelded, Is.False);

        // Second DoAfter cancels the first.
        // Not using helper, because it runs too many ticks & causes the do-after to finish.
        await Server.WaitPost(() => InteractSys.UserInteraction(Player, TargetCoords, Target));
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        Assert.That(comp.IsWelded, Is.False);

        // Third do after will work fine
        await Interact(Weld);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        Assert.That(comp.IsWelded, Is.True);

        // Repeat test for un-welding
        await Interact(Weld, awaitDoAfters:false);
        await RunTicks(1);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
        Assert.That(comp.IsWelded, Is.True);
        await Server.WaitPost(() => InteractSys.UserInteraction(Player, TargetCoords, Target));
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        Assert.That(comp.IsWelded, Is.True);
        await Interact(Weld);
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
        Assert.That(comp.IsWelded, Is.False);
    }
}
