using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Research
{
    public partial class SharedLatheDatabaseComponentData : ISerializationHooks
    {
        [DataField("recipes")] private List<string> _recipeIds = new();

        [DataClassTarget("recipes")]
        private readonly List<LatheRecipePrototype> _recipes = new();

        public void BeforeSerialization()
        {
            var list = new List<string>();

            foreach (var recipe in _recipes)
            {
                list.Add(recipe.ID);
            }

            _recipeIds = list;
        }

        public void AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in _recipeIds)
            {
                if (prototypeManager.TryIndex(id, out LatheRecipePrototype recipe))
                {
                    _recipes.Add(recipe);
                }
            }
        }
    }
}
