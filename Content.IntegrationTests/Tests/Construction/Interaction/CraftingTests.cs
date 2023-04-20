using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Stacks;
using NUnit.Framework;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class CraftingTests : InteractionTest
{
    public const string ShardGlass = "ShardGlass";
    public const string Spear = "Spear";

    /// <summary>
    /// Craft a simple instant recipe
    /// </summary>
    [Test]
    public async Task CraftRods()
    {
        await PlaceInHands(Steel);
        await CraftItem(Rod);
        await FindEntity((Rod, 2));
    }

    /// <summary>
    /// Craft a simple recipe with a DoAfter
    /// </summary>
    [Test]
    public async Task CraftGrenade()
    {
        await PlaceInHands(Steel, 5);
        await CraftItem("ModularGrenadeRecipe");
        await FindEntity("ModularGrenade");
    }

    /// <summary>
    /// Craft a complex recipe (more than one ingredient).
    /// </summary>
    [Test]
    public async Task CraftSpear()
    {
        // Spawn a full tack of rods in the user's hands.
        await PlaceInHands(Rod, 10);
        await SpawnEntity((Cable, 10), PlayerCoords);

        // Attempt (and fail) to craft without glass.
        await CraftItem(Spear, shouldSucceed: false);
        await FindEntity(Spear, shouldSucceed: false);

        // Spawn three shards of glass and finish crafting (only one is needed).
        await SpawnTarget(ShardGlass);
        await SpawnTarget(ShardGlass);
        await SpawnTarget(ShardGlass);
        await CraftItem(Spear);
        await FindEntity(Spear);

        // Player's hands should be full of the remaining rods, except those dropped during the failed crafting attempt.
        // Spear and left over stacks should be on the floor.
        await AssertEntityLookup((Rod, 2), (Cable, 8), (ShardGlass, 2), (Spear, 1));
    }

    // The following is wrapped in an if DEBUG. This is because of cursed state handling bugs. Tests don't (de)serialize
    // net messages and just copy objects by reference. This means that the server will directly modify cached server
    // states on the client's end. Crude fix at the moment is to used modified state handling while in debug mode
    // Otherwise, this test cannot work.
#if DEBUG
    /// <summary>
    /// Cancel crafting a complex recipe.
    /// </summary>
    [Test]
    public async Task CancelCraft()
    {
        var rods = await SpawnEntity((Rod, 10), TargetCoords);
        var wires = await SpawnEntity((Cable, 10), TargetCoords);
        var shard = await SpawnEntity(ShardGlass, TargetCoords);

        var rodStack = SEntMan.GetComponent<StackComponent>(rods);
        var wireStack = SEntMan.GetComponent<StackComponent>(wires);

        await RunTicks(5);
        var sys = SEntMan.System<SharedContainerSystem>();
        Assert.That(sys.IsEntityInContainer(rods), Is.False);
        Assert.That(sys.IsEntityInContainer(wires), Is.False);
        Assert.That(sys.IsEntityInContainer(shard), Is.False);

        await Server.WaitPost(() => SConstruction.TryStartItemConstruction(Spear, Player));
        await RunTicks(1);

        // DoAfter is in progress. Entity not spawned, stacks have been split and someingredients are in a container.
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
        Assert.That(sys.IsEntityInContainer(shard), Is.True);
        Assert.That(sys.IsEntityInContainer(rods), Is.False);
        Assert.That(sys.IsEntityInContainer(wires), Is.False);
        Assert.That(rodStack.Count, Is.EqualTo(8));
        Assert.That(wireStack.Count, Is.EqualTo(8));
        await FindEntity(Spear, shouldSucceed: false);

        // Cancel the DoAfter. Should drop ingredients to the floor.
        await CancelDoAfters();
        Assert.That(sys.IsEntityInContainer(rods), Is.False);
        Assert.That(sys.IsEntityInContainer(wires), Is.False);
        Assert.That(sys.IsEntityInContainer(shard), Is.False);
        await FindEntity(Spear, shouldSucceed: false);
        await AssertEntityLookup((Rod, 10), (Cable, 10), (ShardGlass, 1));

        // Re-attempt the do-after
        await Server.WaitPost(() => SConstruction.TryStartItemConstruction(Spear, Player));
        await RunTicks(1);

        // DoAfter is in progress. Entity not spawned, ingredients are in a container.
        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(1));
        Assert.That(sys.IsEntityInContainer(shard), Is.True);
        await FindEntity(Spear, shouldSucceed: false);

        // Finish the DoAfter
        await AwaitDoAfters();

        // Spear has been crafted. Rods and wires are no longer contained. Glass has been consumed.
        await FindEntity(Spear);
        Assert.That(sys.IsEntityInContainer(rods), Is.False);
        Assert.That(sys.IsEntityInContainer(wires), Is.False);
        Assert.That(SEntMan.Deleted(shard));
    }
#endif
}

