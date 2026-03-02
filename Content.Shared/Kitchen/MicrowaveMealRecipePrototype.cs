using System.Linq;
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
    public sealed partial class FoodRecipePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The name of the recipe.
        /// </summary>
        /// <remarks>
        ///     This is used to sort recipes in alphabetical order in the guidebook.
        /// </remarks>
        [DataField]
        public string Name = string.Empty;

        /// <summary>
        ///     The guidebook grouping for this recipe.
        /// </summary>
        [DataField]
        public string Group = "Other";

        /// <summary>
        ///     The reagent ingredients used in this recipe.
        /// </summary>
        [DataField("reagents")]
        private ReagentQuantity[] _ingsReagents = [];

        /// <summary>
        ///     The solid ingredients used in this recipe.
        /// </summary>
        [DataField("solids", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, EntityPrototype>))]
        private Dictionary<string, FixedPoint2> _ingsSolids = new();

        /// <summary>
        ///     The resulting entity made from this recipe.
        /// </summary>
        [DataField("result", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Result { get; private set; } = string.Empty;

        /// <summary>
        ///     The cooking time of this recipe.
        /// </summary>
        [DataField("time")]
        public uint CookTime { get; private set; } = 5;

        public IReadOnlyList<ReagentQuantity> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, FixedPoint2> IngredientsSolids => _ingsSolids;

        /// <summary>
        ///     Is this recipe unavailable in normal circumstances?
        /// </summary>
        [DataField]
        public bool SecretRecipe = false;

        /// <summary>
        ///    Count the number of ingredients in a recipe for sorting the recipe list.
        ///    This makes sure that where ingredient lists overlap, the more complex
        ///    recipe is picked first.
        /// </summary>
        public FixedPoint2 IngredientCount()
        {
            var solidCount = _ingsSolids.Select(s => s.Value).Sum();
            var reagentCount = _ingsReagents.Length;

            return solidCount + reagentCount;
        }
    }
}
