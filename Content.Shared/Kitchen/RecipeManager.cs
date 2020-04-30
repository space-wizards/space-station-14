using System;
using System.Collections.Generic;
using Content.Shared.Prototypes.Kitchen;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen
{
    
    public class RecipeManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649
        public List<MealRecipePrototype> Recipes { get; private set; }

        public void Initialize()
        {
            Recipes = new List<MealRecipePrototype>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<MealRecipePrototype>())
            {
                Recipes.Add(item);
            }

            Recipes.Sort(new RecipeComparer());
        }
        private class RecipeComparer : IComparer<MealRecipePrototype>
        {
            int IComparer<MealRecipePrototype>.Compare(MealRecipePrototype x, MealRecipePrototype y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                if (x.Ingredients.Count < y.Ingredients.Count)
                {
                    return 1;
                }

                if (x.Ingredients.Count > y.Ingredients.Count)
                {
                    return -1;
                }

                return 0;
            }


        }
    }
}
