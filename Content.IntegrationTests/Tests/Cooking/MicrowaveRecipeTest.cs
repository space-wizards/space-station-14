using Content.IntegrationTests.Utility;
using Content.Server.Kitchen.EntitySystems;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Cooking;

/// <summary>
///     Integration tests related to microwaves and microwave recipes.
/// </summary>
public sealed class MicrowaveRecipeTest
{
    private static readonly string[] FoodRecipes = GameDataScrounger.PrototypesOfKind<FoodRecipePrototype>();
    private static readonly EntProtoId MicrowavePrototype = "KitchenMicrowave";

    [Test]
    [TestOf(typeof(MicrowaveSystem))]
    [TestCaseSource(nameof(FoodRecipes))]
    [Description("Checks whether a microwave recipe's ingredients will create that recipe in the microwave.")]
    public async Task AllRecipeIngredientsMakeRecipe(string protoKey)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var entMan = server.EntMan;
        var microwaveSystem = entMan.System<MicrowaveSystem>();
        var transformSystem = entMan.System<SharedTransformSystem>();

        var testMap = await pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            // Spawn the microwave we will use for our recipes.
            var microwave = entMan.Spawn(MicrowavePrototype, coordinates: testMap.MapCoords);
            var microwaveString = entMan.ToPrettyString(microwave);

            Assert.That(entMan.TryGetComponent<MicrowaveComponent>(microwave, out var comp),
                $"Microwave entity {microwaveString} lacks a {nameof(MicrowaveComponent)}!");

            // Get the parameters we need to make this recipe.
            const uint multiplePortionCount = 6;
            var proto = protoMan.Index<FoodRecipePrototype>(protoKey);

            // Ensure this recipe is provided to the microwave if this is a secret recipe.
            if (proto.SecretRecipe)
            {
                var recipeProvider = entMan.EnsureComponent<FoodRecipeProviderComponent>(microwave);
                recipeProvider.ProvidedRecipes.Add(protoKey);
            }

            // First, test that a single portion works.
            ValidateRecipePortions(proto, 1, microwave, microwaveSystem, entMan);

            // Then, test that making multiple portions of the same recipe works.
            ValidateRecipePortions(proto, multiplePortionCount, microwave, microwaveSystem, entMan);
        });

        await pair.CleanReturnAsync();
    }

    private void ValidateRecipePortions(FoodRecipePrototype prototype,
        uint portions,
        EntityUid microwave,
        MicrowaveSystem microwaveSystem,
        EntityManager entMan)
    {
        var ingredients = prototype.Ingredients * portions;
        var cookTIme = prototype.CookTime * portions;
        var recipe = microwaveSystem.GetRecipe(microwave, ingredients, cookTIme);
        var microwaveString = entMan.ToPrettyString(microwave);
        var recipeDebugString = $"Ingredients for {nameof(FoodRecipePrototype)} {prototype.ID}";

        using (Assert.EnterMultipleScope())
        {
            // Tried to get a recipe for these ingredients, but no valid recipe was found.
            Assert.That(recipe.recipe, Is.Not.Null,
                $"{recipeDebugString} did not resolve to a recipe in {microwaveString} in {portions} portions!");

            // Resulted in a different recipe instead.
            Assert.That(recipe.recipe.ID, Is.EqualTo(prototype.ID),
                $"{recipeDebugString} resulted in an incorrect recipe for {microwaveString} in {portions} portions!");

            // Recipe portion count does not match the amount we're trying to make.
            Assert.That(recipe.count, Is.EqualTo(portions),
                $"{recipeDebugString} resulted in {recipe.count} recipe portions for {microwaveString}! Expected: {portions}");
        }
    }
}
