using System.Linq;
using Content.Shared.Kitchen;

namespace Content.IntegrationTests.Tests.Microwave;

public sealed class KillMicrowaveTest
{
    [Test]
    public async Task PleaseStopAddingNewMicrowaveRecipes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ProtoMan;

        var recipesCount = protoMan.EnumeratePrototypes<FoodRecipePrototype>().Count();
        var recipesLimit = 218;

        if (recipesCount > recipesLimit)
        {
            Assert.Fail($"PLEASE STOP ADDING NEW MICROWAVE RECIPES. MICROWAVE RECIPES ARE FROZEN AND NEED TO BE REPLACED WITH PROPER COOKING MECHANICS! See https://github.com/space-wizards/space-station-14/issues/8524. Keep it under {recipesLimit}. Current count: {recipesCount}");
        }

        if (recipesCount < recipesLimit)
        {
            Assert.Fail($"Oh, you deleted the microwave recipes? YOU ARE SO COOL! Please lower the number of recipes in KillMicrowaveTest from {recipesLimit} to {recipesCount} so that future contributors cannot add new recipes back.");
        }

        await pair.CleanReturnAsync();
    }
}
