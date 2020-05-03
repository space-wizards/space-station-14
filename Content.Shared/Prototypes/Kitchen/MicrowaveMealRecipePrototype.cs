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

        private string _id;
        public string Name => Loc.GetString(Name);
        private string _name;
        public string Result;
        public int CookTime;
        public IReadOnlyDictionary<string, int> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, int> IngredientsSolids => _ingsSolids;

        private Dictionary<string, int> _ingsReagents;
        private Dictionary<string, int> _ingsSolids;


        public string ID => _id;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref Result, "result", string.Empty);
            serializer.DataField(ref _ingsReagents, "reagents", new Dictionary<string, int>());
            serializer.DataField(ref _ingsSolids, "solids", new Dictionary<string, int>());
            serializer.DataField(ref CookTime, "time", 5);
        }

    }
}
