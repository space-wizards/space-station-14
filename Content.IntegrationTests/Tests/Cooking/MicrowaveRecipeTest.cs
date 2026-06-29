using Content.IntegrationTests.Utility;
using Content.Server.Kitchen.Components;
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
            var proto = protoMan.Index<FoodRecipePrototype>(protoKey);
            var ingredients = proto.Ingredients;
            var cookTime = proto.CookTime;

            // Ensure this recipe is provided to the microwave if this is a secret recipe.
            if (proto.SecretRecipe)
            {
                var recipeProvider = entMan.EnsureComponent<FoodRecipeProviderComponent>(microwave);
                recipeProvider.ProvidedRecipes.Add(protoKey);
            }

            // Get the recipe we *would* make, if we put these ingredients in the microwave.
            var recipe = microwaveSystem.GetRecipe(microwave, ingredients, cookTime);
            var recipeDebugString = $"Ingredients for {nameof(FoodRecipePrototype)} {protoKey}";

            // Tried to get a recipe for these ingredients, but no valid recipe was found.
            Assert.That(recipe.recipe, Is.Not.Null,
                $"{recipeDebugString} did not resolve to a recipe in {microwaveString}!");

            // Resulted in a different recipe instead.
            Assert.That(recipe.recipe.ID, Is.EqualTo(protoKey),
                $"{recipeDebugString} resulted in an incorrect recipe for {microwaveString}!");

            // Recipe portion count is not exactly 1.
            Assert.That(recipe.count, Is.EqualTo(1),
                $"{recipeDebugString} resulted in {recipe.count} recipe portions for {microwaveString}!");
        });

        await pair.CleanReturnAsync();
    }
}
