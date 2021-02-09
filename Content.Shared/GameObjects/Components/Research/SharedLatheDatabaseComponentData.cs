using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public partial class SharedLatheDatabaseComponentData
    {
        [DataClassTarget("recipes")]
        private readonly List<LatheRecipePrototype> _recipes = new();

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction(
                "recipes",
                new List<string>(),
                recipes =>
                {
                    var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                    foreach (var id in recipes)
                    {
                        if (prototypeManager.TryIndex(id, out LatheRecipePrototype recipe))
                        {
                            _recipes.Add(recipe);
                        }
                    }
                },
                () =>
                {
                    var list = new List<string>();

                    foreach (var recipe in _recipes)
                    {
                        list.Add(recipe.ID);
                    }

                    return list;
                });

        }
    }
}
