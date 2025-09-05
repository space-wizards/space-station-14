using System.Linq;
using Content.Shared.Kitchen;

namespace Content.IntegrationTests.Tests.Microwave;

[TestFixture]
public sealed class KillMicrowaveTest
{
    [Test]
    public async Task PleaseStopAddingNewMicrowaveRecipes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ProtoMan;

        var recipesCount = protoMan.EnumeratePrototypes<FoodRecipePrototype>().Count();
        var recipesLimit = 218; //Current count as of 2025-09-05

        Assert.That(recipesCount <= recipesLimit, $"PLEASE STOP ADDING NEW MICROWAVE RECIPES. THIS SHIT IS OBSOLETED! Keep it under {recipesLimit}. Current count: {recipesCount}");

        await pair.CleanReturnAsync();
    }
}
