using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Nutrition;

[TestFixture]
public sealed class HungerThirstTest : InteractionTest
{
    private readonly EntProtoId _drink = "DrinkLemonadeGlass";
    private readonly EntProtoId _food = "FoodCakeVanillaSlice";
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task HungerThirstIncreaseDecreaseTest()
    {
        // Ensure that the player can breathe and not suffocate
        await AddAtmosphere();

        HungerComponent hungerComponent = Comp<HungerComponent>(Player);
        Assert.That(hungerComponent, Is.Not.Null);

        ThirstComponent thirstComponent = Comp<ThirstComponent>(Player);
        Assert.That(thirstComponent, Is.Not.Null);

        HungerSystem hungerSystem = SEntMan.System<HungerSystem>();
        Assert.That(hungerSystem, Is.Not.Null);

        ThirstSystem thirstSystem = SEntMan.System<ThirstSystem>();
        Assert.That(thirstSystem, Is.Not.Null);

        hungerSystem.SetHunger(SPlayer, hungerComponent.Thresholds[HungerThreshold.Okay], hungerComponent);
        var previousHungerValue = hungerSystem.GetHunger(hungerComponent);

        thirstSystem.SetThirst(SPlayer, thirstComponent, thirstComponent.ThirstThresholds[ThirstThreshold.Okay]);
        var previousThirstValue = thirstComponent.CurrentThirst;

        previousHungerValue = await CheckHungerDecrease(hungerComponent, hungerSystem, previousHungerValue);
        previousThirstValue = await CheckThirstDecrease(thirstComponent, thirstSystem, previousThirstValue);

        await CheckHungerIncrease(hungerComponent, hungerSystem, previousHungerValue);
        await CheckThirstIncrease(thirstComponent, previousThirstValue);
    }

    private async Task CheckThirstIncrease(ThirstComponent thirstComponent, float previousThirstValue)
    {
        await DeleteHeldEntity();
        await PlaceInHands(_drink);

        // We ensure the drink is there
        EntityUid? drink = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(drink, Is.Not.Null);

        // Get the solution that can be consumed
        IngestionSystem ingestionSystem = SEntMan.System<IngestionSystem>();
        Entity<SolutionComponent>? solution;
        Assert.That(ingestionSystem.CanConsume(SPlayer, SPlayer, drink.Value, out solution, out _));

        // Find the initial amount of solution in the drink
        var initialSolutionVolume = ((SolutionComponent)solution).Solution.Volume;

        // We drink the drink in hand
        await UseInHand();

        // To see a change in thirst, we need to wait at least 30 seconds
        await RunSeconds(30);

        // Ensure the solution volume has decreased
        Assert.That(((SolutionComponent)solution).Solution.Volume, Is.LessThan(initialSolutionVolume));

        var currentThirstValue = thirstComponent.CurrentThirst;
        Assert.That(currentThirstValue, Is.GreaterThan(previousThirstValue));
    }

    private async Task CheckHungerIncrease(HungerComponent hungerComponent, HungerSystem hungerSystem, float previousHungerValue)
    {
        await DeleteHeldEntity();
        await PlaceInHands(_food);

        // We ensure the food is there
        EntityUid? food = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(food, Is.Not.Null);

        // We eat the food in hand
        await UseInHand();

        // To see a change in hunger, we need to wait at least 30 seconds
        await RunSeconds(30);

        // We ensure the food is fully eaten
        EntityUid? foodEaten = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(foodEaten, Is.Null);

        var currentHungerValue = hungerSystem.GetHunger(hungerComponent);
        Assert.That(currentHungerValue, Is.GreaterThan(previousHungerValue));
    }

    private async Task<float> CheckThirstDecrease(ThirstComponent thirstComponent, ThirstSystem thirstSystem, float previousThirstValue)
    {
        previousThirstValue = thirstComponent.CurrentThirst;

        await RunTicks(20);

        var currentThirstValue = thirstComponent.CurrentThirst;
        Assert.That(currentThirstValue, Is.LessThan(previousThirstValue));
        previousThirstValue = currentThirstValue;
        return previousThirstValue;
    }

    private async Task<float> CheckHungerDecrease(HungerComponent hungerComponent, HungerSystem hungerSystem, float previousHungerValue)
    {
        previousHungerValue = hungerSystem.GetHunger(hungerComponent);

        await RunTicks(10);

        var currentHungerValue = hungerSystem.GetHunger(hungerComponent);
        Assert.That(currentHungerValue, Is.LessThan(previousHungerValue));
        previousHungerValue = currentHungerValue;
        return previousHungerValue;
    }
}
