using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Kitchen.EntitySystems;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Kitchen;

/// <summary>
///     Integration tests related to microwaves and microwave recipes.
/// </summary>
public sealed class MicrowaveRecipeTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly MicrowaveSystem _microwave = null!;

    private static readonly string[] Recipes = GameDataScrounger.PrototypesOfKind<MicrowaveMealRecipePrototype>();
    private static readonly EntProtoId MicrowavePrototype = "KitchenMicrowave";
    private const uint MaxSeconds = 30;

    [Test]
    [TestOf(typeof(MicrowaveSystem))]
    [TestCaseSource(nameof(Recipes))]
    [Description("Checks whether a microwave recipe's ingredients will create that recipe in the microwave.")]
    public async Task AllRecipeIngredientsMakeRecipe(string protoKey)
    {
        var server = Pair.Server;

        // Spawn the microwave we will use for our recipes.
        var microwave = await Spawn(MicrowavePrototype);
        var microwaveString = SEntMan.ToPrettyString(microwave);

        await server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<MicrowaveComponent>(microwave),
                $"Microwave entity {microwaveString} lacks a {nameof(MicrowaveComponent)}!");

            // Get the parameters we need to make this recipe.
            var proto = SProtoMan.Index<MicrowaveMealRecipePrototype>(protoKey);
            var maxPortions = MaxSeconds / proto.Time;

            // Ensure this recipe is provided to the microwave if this is a secret recipe.
            if (proto.SecretRecipe)
            {
                var recipeProvider = SEntMan.EnsureComponent<FoodRecipeProviderComponent>(microwave);
                recipeProvider.ProvidedRecipes.Add(protoKey);
            }

            // First, test that a single portion works.
            ValidateRecipePortions(proto, 1, microwave);

            // Then, test that making multiple portions of the same recipe works.
            if (maxPortions > 1)
                ValidateRecipePortions(proto, maxPortions, microwave);
        });
    }

    private void ValidateRecipePortions(MicrowaveMealRecipePrototype prototype, uint portions, EntityUid microwave)
    {
        var ingredients = prototype.Ingredients * portions;
        var cookTime = prototype.Time * portions;
        var portionedRecipe = _microwave.GetRecipe(microwave, ingredients, cookTime);
        var microwaveString = SEntMan.ToPrettyString(microwave);
        var recipeDebugString = $"Ingredients for {nameof(MicrowaveMealRecipePrototype)} {prototype.ID}";

        using (Assert.EnterMultipleScope())
        {
            // Tried to get a recipe for these ingredients, but no valid recipe was found.
            Assert.That(portionedRecipe, Is.Not.Null,
                $"{recipeDebugString} did not resolve to a recipe in {microwaveString} in {portions} portions!");

            var recipe = portionedRecipe.Value.Recipe;
            var count = portionedRecipe.Value.Count;

            // Resulted in a different recipe instead.
            Assert.That(recipe, Is.EqualTo(prototype.ID),
                $"{recipeDebugString} resulted in an incorrect recipe for {microwaveString} in {portions} portions!");

            // Recipe portion count does not match the amount we're trying to make.
            Assert.That(count, Is.EqualTo(portions),
                $"{recipeDebugString} resulted in {count} recipe portions for {microwaveString}! Expected: {portions}");
        }
    }
}
