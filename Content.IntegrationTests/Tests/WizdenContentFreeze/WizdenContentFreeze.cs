using Content.Shared.Kitchen;

namespace Content.IntegrationTests.Tests.WizdenContentFreeze;

/// <summary>
/// These tests are limited to adding a specific type of content, essentially freezing it. If you are a fork developer, you may want to disable these tests.
/// </summary>
public sealed class WizdenContentFreeze
{
    /// <summary>
    /// This freeze prohibits the addition of new microwave recipes.
    /// The maintainers decided that the mechanics of cooking food in the microwave should be removed,
    /// and all recipes should be ported to other cooking methods.
    /// All added recipes essentially increase the technical debt of future cooking refactoring.
    ///
    /// https://github.com/space-wizards/space-station-14/issues/8524
    /// </summary>
    [Test]
    public async Task MicrowaveRecipesFreezeTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ProtoMan;

        var recipesCount = protoMan.Count<FoodRecipePrototype>();
        var recipesLimit = 218;

        if (recipesCount > recipesLimit)
        {
            Assert.Fail($"PLEASE STOP ADDING NEW MICROWAVE RECIPES. MICROWAVE RECIPES ARE FROZEN AND NEED TO BE REPLACED WITH PROPER COOKING MECHANICS! See https://github.com/space-wizards/space-station-14/issues/8524. Keep it under {recipesLimit}. Current count: {recipesCount}");
        }

        if (recipesCount < recipesLimit)
        {
            Assert.Fail($"Oh, you deleted the microwave recipes? YOU ARE SO COOL! Please lower the number of recipes in MicrowaveRecipesFreezeTest from {recipesLimit} to {recipesCount} so that future contributors cannot add new recipes back.");
        }

        await pair.CleanReturnAsync();
    }
}
