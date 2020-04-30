using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>

    [Prototype("microwaveMealRecipe")]

    public class MicrowaveMealRecipePrototype : IPrototype, IIndexedPrototype
    {

        private string _id;
        private string _name;
        private string _result;
        private Dictionary<string, int> _ingredients;

        public string ID => _id;
        public string Name => Loc.GetString(_name);
        public string Result => _result;
        public IReadOnlyDictionary<string, int> Ingredients => _ingredients;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _result, "result", string.Empty);
            serializer.DataField(ref _ingredients, "ingredients", new Dictionary<string, int>());
        }
    }
}
