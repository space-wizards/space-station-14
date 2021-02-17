using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>

    [Prototype("microwaveMealRecipe")]

    public class FoodRecipePrototype : IPrototype, IIndexedPrototype
    {

        [YamlField("id")]
        private string _id;
        [YamlField("name")]
        private string _name;
        [YamlField("result")]
        private string _result;
        [YamlField("time")]
        private int _cookTime = 5;

        [YamlField("reagents")] private Dictionary<string, int> _ingsReagents = new();
        [YamlField("solids")]
        private Dictionary<string, int> _ingsSolids = new ();

        public string Name => Loc.GetString(_name);
        public string ID => _id;
        public string Result => _result;
        public int CookTime => _cookTime;
        public IReadOnlyDictionary<string, int> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, int> IngredientsSolids => _ingsSolids;
    }
}
