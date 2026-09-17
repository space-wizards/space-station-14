using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen;

public sealed partial class RecipeManager : EntitySystem
{
    public List<FoodRecipePrototype> Recipes { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        ReloadRecipes();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<FoodRecipePrototype>())
            ReloadRecipes();
    }

    private void ReloadRecipes()
    {
        Recipes = ProtoMan
            .EnumeratePrototypes<FoodRecipePrototype>()
            .Where(x => !x.SecretRecipe)
            .OrderByDescending(x => x.IngredientCount())
            .ToList();
    }
}
