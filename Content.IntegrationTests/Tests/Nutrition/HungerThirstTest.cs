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

        HungerComponent hungerComponent = Comp<HungerComponent>(Player);
        ThirstComponent thirstComponent = Comp<ThirstComponent>(Player);
        HungerSystem hungerSystem = SEntMan.System<HungerSystem>();
        ThirstSystem thirstSystem = SEntMan.System<ThirstSystem>();

        hungerSystem.SetHunger(SPlayer, hungerComponent.Thresholds[HungerThreshold.Okay], hungerComponent);
        var previousHungerValue = hungerSystem.GetHunger(hungerComponent);

        thirstSystem.SetThirst(SPlayer, thirstComponent, thirstComponent.ThirstThresholds[ThirstThreshold.Okay]);
        var previousThirstValue = thirstComponent.CurrentThirst;

        // Ensure hunger value decrease over time (the Urist gets hungrier)
        previousHungerValue = hungerSystem.GetHunger(hungerComponent);
        await RunSeconds((float)(hungerComponent.ThresholdUpdateRate + TimeSpan.FromSeconds(1)).TotalSeconds);

        var currentHungerValue = hungerSystem.GetHunger(hungerComponent);
        Assert.That(currentHungerValue, Is.LessThan(previousHungerValue), "Hunger value did not decrease over time");
        previousHungerValue = currentHungerValue;

        // Ensure thrist value decrease over time (the Urist gets thirstier)
        previousThirstValue = thirstComponent.CurrentThirst;
        await RunSeconds((float)(thirstComponent.UpdateRate + TimeSpan.FromSeconds(1)).TotalSeconds);

        var currentThirstValue = thirstComponent.CurrentThirst;
        Assert.That(currentThirstValue, Is.LessThan(previousThirstValue), "Thirst value did not decrease over time");
        previousThirstValue = currentThirstValue;

        // Now we spawn food in the Urist's hand
        await DeleteHeldEntity();
        await PlaceInHands(_food);

        // We ensure the food is there
        EntityUid? food = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(food, Is.Not.Null, "Food was not spawned in the Urist hand");

        // We eat the food in hand
        await UseInHand();

        // To see a change in hunger, we need to wait at least 30 seconds
        await RunSeconds(30);

        // We ensure the food is fully eaten
        EntityUid? foodEaten = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(foodEaten, Is.Null, "Food item did not disapear after eating it");

        // Ensure that the hunger value has increased (The Urist is less hungry)
        Assert.That(hungerSystem.GetHunger(hungerComponent), Is.GreaterThan(previousHungerValue), "Hunger value did not increase after eating food");

        // Now we spawn drink in the Urist's hand
        await DeleteHeldEntity();
        await PlaceInHands(_drink);

        // We ensure the drink is there
        EntityUid? drink = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(drink, Is.Not.Null, "Drink did not spawn in the Urist hand");

        // Get the solution that can be consumed
        IngestionSystem ingestionSystem = SEntMan.System<IngestionSystem>();
        Entity<SolutionComponent>? solution;
        Assert.That(ingestionSystem.CanConsume(SPlayer, SPlayer, drink.Value, out solution, out _),
            "Unable to get the solution or the entity can not be consumed");

        // Find the initial amount of solution in the drink
        var initialSolutionVolume = ((SolutionComponent)solution).Solution.Volume;

        // We drink the drink in hand
        await UseInHand();

        // To see a change in thirst, we need to wait at least 30 seconds
        await RunSeconds(30);

        // Ensure the solution volume has decreased
        Assert.That(((SolutionComponent)solution).Solution.Volume, Is.LessThan(initialSolutionVolume), "Solution volume did not decrease after drinking");

        // Ensure that the thirst value has increased (The Urist is less thristy)
        Assert.That(thirstComponent.CurrentThirst, Is.GreaterThan(previousThirstValue), "Thirst value did not increase after drinking");
    }
}
