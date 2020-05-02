using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>

    [Prototype("microwaveMealRecipe")]

    public class FoodRecipePrototype : IPrototype, IIndexedPrototype
    {

        public string _id;
        public string _name => Loc.GetString(Name);
        private string Name;
        public string _result;
        public IReadOnlyDictionary<string, int> _ingReagents => IngredientsReagents;
        public IReadOnlyDictionary<string, int> _ingSolids => IngredientsSolids;

        private Dictionary<string, int> IngredientsReagents;
        private Dictionary<string, int> IngredientsSolids;
        public int _cookTime;

        public string ID => _id;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref Name, "name", string.Empty);
            serializer.DataField(ref _result, "result", string.Empty);
            serializer.DataField(ref IngredientsReagents, "reagents", new Dictionary<string, int>());
            serializer.DataField(ref IngredientsSolids, "solids", new Dictionary<string, int>());
            serializer.DataField(ref _cookTime, "time", 5);
        }

    }
}
