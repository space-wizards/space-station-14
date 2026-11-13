#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Kitchen;

namespace Content.IntegrationTests.Tests.WizdenContentFreeze;

/// <summary>
/// These tests are limited to adding a specific type of content, essentially freezing it. If you are a fork developer, you may want to disable these tests.
/// </summary>
public sealed class WizdenContentFreeze : GameTest
{
    /// <summary>
    /// The current number of microwave recipes in the freeze.
    /// If any are removed, update this count.
    /// </summary>
    public const int RecipesLimit = 218;

    /// <summary>
    /// This freeze prohibits the addition of new microwave recipes.
    /// The maintainers decided that the mechanics of cooking food in the microwave should be removed,
    /// and all recipes should be ported to other cooking methods.
    /// All added recipes essentially increase the technical debt of future cooking refactoring.
    ///
    /// https://github.com/space-wizards/space-station-14/issues/8524
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    [Description("Ensures that no new microwave recipes are added while the recipes remain frozen.")]
    public async Task MicrowaveRecipesFreezeTest()
    {
        var recipesCount = SProtoMan.Count<FoodRecipePrototype>();

        Assert.That(recipesCount, Is.Not.GreaterThan(RecipesLimit),
            $"Do not add more new microwave recipes. Microwave recipes are frozen and need to be replaced with proper cooking mechanics. See https://github.com/space-wizards/space-station-14/issues/8524.");

        var recipesCount = protoMan.Count<MicrowaveMealRecipePrototype>();
        var recipesLimit = 219;

        if (recipesCount > recipesLimit)
        {
            Assert.Fail($"PLEASE STOP ADDING NEW MICROWAVE RECIPES. MICROWAVE RECIPES ARE FROZEN AND NEED TO BE REPLACED WITH PROPER COOKING MECHANICS! See https://github.com/space-wizards/space-station-14/issues/8524. Keep it under {recipesLimit}. Current count: {recipesCount}");
        }

        if (recipesCount < recipesLimit)
        {
            Assert.Fail($"Oh, you deleted the microwave recipes? YOU ARE SO COOL! Please lower the number of recipes in MicrowaveRecipesFreezeTest from {recipesLimit} to {recipesCount} so that future contributors cannot add new recipes back.");
        }
    }
}
