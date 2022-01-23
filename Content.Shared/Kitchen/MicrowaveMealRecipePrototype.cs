using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Kitchen
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

        [DataField("reagents", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<uint, ReagentPrototype>))]
        private readonly Dictionary<string, uint> _ingsReagents = new();

        [DataField("solids", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
        private readonly Dictionary<string, uint> _ingsSolids = new ();

        [DataField("result", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Result { get; } = string.Empty;

        [DataField("time")]
        public uint CookTime { get; } = 5;

        public string Name => Loc.GetString(_name);

        public IReadOnlyDictionary<string, uint> IngredientsReagents => _ingsReagents;
        public IReadOnlyDictionary<string, uint> IngredientsSolids => _ingsSolids;
    }
}
