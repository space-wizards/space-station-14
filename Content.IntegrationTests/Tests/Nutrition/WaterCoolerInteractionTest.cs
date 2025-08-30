using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Nutrition;

public sealed class WaterCoolerInteractionTest : InteractionTest
{
    /// <summary>
    /// ProtoId of the water cooler entity.
    /// </summary>
    private static readonly EntProtoId WaterCooler = "WaterCooler";

    /// <summary>
    /// ProtoId of the paper cup entity dispensed by the water cooler.
    /// </summary>
    private static readonly EntProtoId PaperCup = "DrinkWaterCup";

    /// <summary>
    /// ProtoId of the water reagent that is stored in the water cooler.
    /// </summary>
    private static readonly ProtoId<ReagentPrototype> Water = "Water";

    /// <summary>
    /// Spawns a water cooler and tests that the player can retrieve a paper cup
    /// by interacting with it, and can return the paper cup by alt-interacting with it.
    /// </summary>
    [Test]
    public async Task GetAndReturnCup()
    {
        // Spawn the water cooler
        var cooler = await SpawnTarget(WaterCooler);

        // Record how many paper cups are in the cooler
        var binComp = Comp<BinComponent>(cooler);
        var initialCount = binComp.Items.Count;
        Assert.That(binComp.Items, Is.Not.Empty, "Water cooler didn't start with any cups");

        // Interact with the water cooler using an empty hand to grab a paper cup
        await Interact();

        var cup = HandSys.GetActiveItem((SPlayer, Hands));

        Assert.Multiple(() =>
        {
            // Make sure the player is now holding a cup
            Assert.That(cup, Is.Not.Null, "Player's hand is empty");
            AssertPrototype(PaperCup, SEntMan.GetNetEntity(cup));

            // Make sure the number of cups in the cooler has decreased by one
            Assert.That(binComp.Items, Has.Count.EqualTo(initialCount - 1), "Number of cups in cooler bin did not decrease by one");

            // Make sure the cup isn't somehow still in the cooler too
            Assert.That(binComp.Items, Does.Not.Contain(cup));
        });

        // Alt-interact with the water cooler while holding the cup to put it back
        await Interact(altInteract: true);

        Assert.Multiple(() =>
        {
            // Make sure the player's hand is empty
            Assert.That(HandSys.ActiveHandIsEmpty((SPlayer, Hands)), "Player's hand is not empty");

            // Make sure the count has gone back up by one
            Assert.That(binComp.Items, Has.Count.EqualTo(initialCount), "Number of cups in cooler bin did not return to initial count");

            // Make sure the cup is in the cooler
            Assert.That(binComp.Items, Contains.Item(cup), "Cup was not returned to cooler");
        });
    }

    /// <summary>
    /// Spawns a water cooler and gives the player an empty paper cup.
    /// Tests that the player can put water into the cup by interacting
    /// with the water cooler while holding the cup.
    /// </summary>
    [Test]
    public async Task FillCup()
    {
        var solutionSys = Server.System<SharedSolutionContainerSystem>();

        // Spawn the water cooler
        await SpawnTarget(WaterCooler);

        // Give the player a cup
        var cup = await PlaceInHands(PaperCup);

        // Make the player interact with the water cooler using the held cup
        await Interact();

        // Make sure the cup now contains water
        Assert.That(solutionSys.GetTotalPrototypeQuantity(ToServer(cup), Water), Is.GreaterThan(FixedPoint2.Zero),
            "Cup does not contain any water");
    }
}
