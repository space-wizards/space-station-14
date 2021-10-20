using System.Threading.Tasks;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Kitchen
{
    [TestFixture]
    public class KitchenTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestRecipesValid()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var recipe in protoManager.EnumeratePrototypes<FoodRecipePrototype>())
                {
                    Assert.That(protoManager.HasIndex<EntityPrototype>(recipe.Result), $"Cannot find FoodRecipe result {recipe.Result} in {recipe.ID}");

                    foreach (var (solid, amount) in recipe.IngredientsSolids)
                    {
                        Assert.That(protoManager.HasIndex<EntityPrototype>(solid), $"Cannot find FoodRecipe solid {solid} in {recipe.ID}");
                        Assert.That(amount > 0, $" FoodRecipe {recipe.ID} has invalid solid amount of {amount}");
                    }

                    foreach (var (reagent, amount) in recipe.IngredientsReagents)
                    {
                        Assert.That(protoManager.HasIndex<ReagentPrototype>(reagent), $"Cannot find FoodRecipe reagent {reagent} in {recipe.ID}");
                        Assert.That(amount > 0, $" FoodRecipe {recipe.ID} has invalid reagent amount of {amount}");
                    }

                    Assert.That(recipe.CookTime > 0, $"Cook time of {recipe.CookTime} for FoodRecipe {recipe.ID} is invalid!");
                }
            });
        }
    }
}
