using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Nutrition;

/// <summary>
/// Tests the mechanics of hunger and thirst.
/// </summary>
public sealed class HungerThirstTest : InteractionTest
{
    private readonly EntProtoId _drink = "DrinkLemonadeGlass";
    private readonly EntProtoId _food = "FoodCakeVanillaSlice";
    protected override string PlayerPrototype => "MobHuman";

    /// <summary>
    /// Tests that hunger and thirst values decrease over time (low means hungrier and thirstier).
    /// Tests that hunger and thirst values increase when eating/drinking (high means less hungry and thirsty).
    /// </summary>
    [Test]
    public async Task HungerThirstIncreaseDecreaseTest()
    {
        // Ensure that the player can breathe and not suffocate
        await AddAtmosphere();

        var hungerComponent = Comp<HungerComponent>(Player);
        var thirstComponent = Comp<ThirstComponent>(Player);
        var hungerSystem = SEntMan.System<HungerSystem>();
        var thirstSystem = SEntMan.System<ThirstSystem>();
        var ingestionSystem = SEntMan.System<IngestionSystem>();

        // Set initial value
        hungerSystem.SetHunger(SPlayer, hungerComponent.Thresholds[HungerThreshold.Okay], hungerComponent);
        thirstSystem.SetThirst(SPlayer, thirstComponent, thirstComponent.ThirstThresholds[ThirstThreshold.Okay]);

        // Ensure hunger and thirst value decrease over time (the Urist gets hungrier/thirstier)
        var previousHungerValue = hungerSystem.GetHunger(hungerComponent);
        var previousThirstValue = thirstComponent.CurrentThirst; // TODO: combined sation system with a sane API

        // Simulate long enough for both update loops to run
        var runTime = Math.Max((float)hungerComponent.ThresholdUpdateRate.TotalSeconds, (float)thirstComponent.UpdateRate.TotalSeconds) + 1f;
        await RunSeconds(runTime);

        var currentHungerValue = hungerSystem.GetHunger(hungerComponent);
        Assert.That(currentHungerValue, Is.LessThan(previousHungerValue), "Hunger value did not decrease over time");
        previousHungerValue = currentHungerValue;

        var currentThirstValue = thirstComponent.CurrentThirst;
        Assert.That(currentThirstValue, Is.LessThan(previousThirstValue), "Thirst value did not decrease over time");
        previousThirstValue = currentThirstValue;

        // Now we spawn food in the Urist's hand
        await PlaceInHands(_food);

        // We eat the food in hand
        await UseInHand();

        // To see a change in hunger, we need to wait at least 30 seconds
        await RunSeconds(30);

        // We ensure the food is fully eaten
        var foodEaten = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(foodEaten, Is.Null, "Food item did not disappear after eating it");

        // Ensure that the hunger value has increased (The Urist is less hungry)
        Assert.That(hungerSystem.GetHunger(hungerComponent), Is.GreaterThan(previousHungerValue), "Hunger value did not increase after eating food");

        // Now we spawn a drink in the Urist's hand
        var drink = await PlaceInHands(_drink);

        // Get the solution that can be consumed
        Assert.That(ingestionSystem.CanConsume(SPlayer, SPlayer, ToServer(drink), out var solution, out _),
            "Unable to get the solution or the entity can not be consumed");

        // Find the initial amount of solution in the drink
        var initialSolutionVolume = solution.Value.Comp.Solution.Volume;

        // We drink the drink in hand
        await UseInHand();

        // To see a change in thirst, we need to wait at least 30 seconds
        await RunSeconds(30);

        // Ensure the solution volume has decreased
        Assert.That(solution.Value.Comp.Solution.Volume, Is.LessThan(initialSolutionVolume), "Solution volume did not decrease after drinking");

        // Ensure that the thirst value has increased (The Urist is less thirsty)
        Assert.That(thirstComponent.CurrentThirst, Is.GreaterThan(previousThirstValue), "Thirst value did not increase after drinking");

        // Make sure that the glass did not get deleted after drinking from it
        var glass = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(glass, Is.Not.Null, "Glass got deleted after drinking from it");
    }
}
