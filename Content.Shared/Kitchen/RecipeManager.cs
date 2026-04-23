using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

public sealed class RecipeManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public List<FoodRecipePrototype> Recipes { get; private set; } = new();

    public void Initialize()
    {
        ReloadRecipes();
        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<FoodRecipePrototype>())
            ReloadRecipes();
    }

    private void ReloadRecipes()
    {
        Recipes = _prototypeManager
            .EnumeratePrototypes<FoodRecipePrototype>()
            .Where(x => !x.SecretRecipe)
            .OrderByDescending(x => x.IngredientCount())
            .ToList();
    }
}
