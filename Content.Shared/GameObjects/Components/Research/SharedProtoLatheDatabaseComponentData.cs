using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Research
{
    public partial class SharedProtoLatheDatabaseComponentData : ISerializationHooks
    {
        [DataField("protolatherecipes")]
        public List<string> RecipeIds { get; private set; }

        [DataClassTarget("protolatherecipes")]
        public readonly List<LatheRecipePrototype> ProtolatheRecipes = new();

        public void BeforeSerialization()
        {
            var list = new List<string>();

            foreach (var recipe in ProtolatheRecipes)
            {
                list.Add(recipe.ID);
            }

            RecipeIds = list;
        }

        public void AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in RecipeIds)
            {
                if (prototypeManager.TryIndex(id, out LatheRecipePrototype recipe))
                {
                    ProtolatheRecipes.Add(recipe);
                }
            }
        }
    }
}
