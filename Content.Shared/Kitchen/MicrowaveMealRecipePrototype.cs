using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Kitchen
{
    /// <summary>
    ///    A recipe for space microwaves.
    /// </summary>
    [Prototype("microwaveMealRecipe")]
    public sealed class FoodRecipePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("name")]
        private string _name = string.Empty;

        [DataField("reagents", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
        private readonly Dictionary<string, FixedPoint2> _ingsReagents = new();

        [DataField("solids", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, EntityPrototype>))]
        private readonly Dictionary<string, FixedPoint2> _ingsSolids = new ();

        [DataField("result", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Result { get; } = string.Empty;

        [DataField("time")]
        public uint CookTime { get; } = 5;

        public string Name => Loc.GetString(_name);

        public IReadOnlyDictionary<string, FixedPoint2> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, FixedPoint2> IngredientsSolids => _ingsSolids;

        /// <summary>
        ///    Count the number of ingredients in a recipe for sorting the recipe list.
        ///    This makes sure that where ingredient lists overlap, the more complex
        ///    recipe is picked first.
        /// </summary>
        public FixedPoint2 IngredientCount()
        {
            FixedPoint2 n = 0;
            n += _ingsReagents.Count; // number of distinct reagents
            foreach (FixedPoint2 i in _ingsSolids.Values) // sum the number of solid ingredients
            {
                n += i;
            }
            return n;
        }
    }
}
