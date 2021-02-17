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

        [DataField("id")]
        private string _id;
        [DataField("name")]
        private string _name;
        [DataField("result")]
        private string _result;
        [DataField("time")]
        private int _cookTime = 5;

        [DataField("reagents")] private Dictionary<string, int> _ingsReagents = new();
        [DataField("solids")]
        private Dictionary<string, int> _ingsSolids = new ();

        public string Name => Loc.GetString(_name);
        public string ID => _id;
        public string Result => _result;
        public int CookTime => _cookTime;
        public IReadOnlyDictionary<string, int> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, int> IngredientsSolids => _ingsSolids;
    }
}
