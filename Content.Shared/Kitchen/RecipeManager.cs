using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen
{
    public sealed class RecipeManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public List<FoodRecipePrototype> Recipes { get; private set; } = new();

        public void Initialize()
        {
            Recipes = new List<FoodRecipePrototype>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<FoodRecipePrototype>())
            {
                Recipes.Add(item);
            }

            Recipes.Sort(new RecipeComparer());
        }
        /// <summary>
        /// Check if a prototype ids appears in any of the recipes that exist.
        /// </summary>
        /// <param name="solidIds"></param>
        /// <returns></returns>
        public bool SolidAppears(string solidId)
        {
            foreach(var recipe in Recipes)
            {
                if(recipe.IngredientsSolids.ContainsKey(solidId))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class RecipeComparer : Comparer<FoodRecipePrototype>
        {
            public override int Compare(FoodRecipePrototype? x, FoodRecipePrototype? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                var nx = x.IngredientCount();
                var ny = y.IngredientCount();
                return -nx.CompareTo(ny);
            }
        }
    }
}
