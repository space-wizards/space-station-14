#nullable enable
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>
    [Prototype("microwaveMealRecipe")]
    public class FoodRecipePrototype : IPrototype
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _result = string.Empty;
        private int _cookTime;

        private Dictionary<string, int> _ingsReagents = new();
        private Dictionary<string, int> _ingsSolids = new();

        public string Name => Loc.GetString(_name);
        public string ID => _id;
        public string Result => _result;
        public int CookTime => _cookTime;
        public IReadOnlyDictionary<string, int> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, int> IngredientsSolids => _ingsSolids;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _result, "result", string.Empty);
            serializer.DataField(ref _ingsReagents, "reagents", new Dictionary<string, int>());
            serializer.DataField(ref _ingsSolids, "solids", new Dictionary<string, int>());
            serializer.DataField(ref _cookTime, "time", 5);
        }
    }
}
