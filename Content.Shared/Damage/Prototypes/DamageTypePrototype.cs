using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A single damage type. These types are grouped together in <see cref="DamageGroupPrototype"/>s.
    /// </summary>
    [Prototype("damageType")]
    [Serializable, NetSerializable]
    public sealed class DamageTypePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        /// The price for each 1% damage reduction in armors
        /// </summary>
        [DataField("armorCoefficientPrice")]
        public double ArmorPriceCoefficient { get; set; }

        /// <summary>
        /// The price for each flat damage reduction in armors
        /// </summary>
        [DataField("armorFlatPrice")]
        public double ArmorPriceFlat { get; set; }

        // --===   Wounding/Medical configuration  ===---

        //TODO: update these to use the value protoId dictionary serializer

        //Note: these should be defined in order of severity!
        //surface wounds are wounds on skin or exposed bodyparts
        [DataField("surfaceWounds", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, WoundPrototype>))]
        public SortedDictionary<FixedPoint2,string> SurfaceWounds { get; init; } = new();

        //internal wounds are wounds that are caused when an injury affects internal soft tissue such as organs or flesh
        [DataField("internalWounds", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, WoundPrototype>))]
        public SortedDictionary<FixedPoint2,string>? InternalWounds { get; init; } = new();

        //solid wounds are wounds that get caused when affecting a solid surface/object, such as bones or an exoskeleton
        [DataField("structuralWounds", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, WoundPrototype>))]
        public SortedDictionary<FixedPoint2,string>? StructuralWounds { get; init; } = new();

        //used to calculate how much this damage type propogates the skin
        [DataField("surfacePenMod")] public float SurfacePenModifier = 1.0f;

        //used to calculate how much this damage type propogates a bodypart/flesh
        [DataField("internalPenMod")] public float InternalPenModifier = 1.0f;

        //used to calculate how much this damage type propogates through a bone if it is protecting organs
        [DataField("structurePenMod")] public float StructurePenModifier = 1.0f;

    }
}
