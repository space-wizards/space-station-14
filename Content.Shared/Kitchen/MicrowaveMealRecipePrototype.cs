#nullable enable
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Prototypes.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>
    [Prototype("microwaveMealRecipe")]
    public class FoodRecipePrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        private string _name = string.Empty;

        [DataField("reagents")]
        private readonly Dictionary<string, int> _ingsReagents = new();

        [DataField("solids")]
        private readonly Dictionary<string, int> _ingsSolids = new ();

        [DataField("result")]
        public string Result { get; } = string.Empty;

        [DataField("time")]
        public int CookTime { get; } = 5;

        public string Name => Loc.GetString(_name);

        public IReadOnlyDictionary<string, int> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, int> IngredientsSolids => _ingsSolids;
    }
}
