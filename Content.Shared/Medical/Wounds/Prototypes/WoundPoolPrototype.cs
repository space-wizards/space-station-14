using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds.Prototypes;

[Prototype("woundPool")]
public sealed class WoundPoolPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    [DataField("damageType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DamageTypePrototype>))]
    public string DamageType { get; init; } = default!;

    //Note: these should be defined in order of severity!
    //surface wounds are wounds on skin or exposed bodyparts
    [DataField("surfaceWounds",
        customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<float, WoundPrototype>))]
    public SortedDictionary<float, string> SurfaceWounds { get; init; } = new();

    //internal wounds are wounds that are caused when an injury affects internal soft tissue such as organs or flesh
    [DataField("internalWounds",
        customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<float, WoundPrototype>))]
    public SortedDictionary<float, string>? InternalWounds { get; init; } = new();

    //solid wounds are wounds that get caused when affecting a solid surface/object, such as bones or an exoskeleton
    [DataField("structuralWounds",
        customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<float, WoundPrototype>))]
    public SortedDictionary<float, string>? StructuralWounds { get; init; } = new();

    //used to calculate how much this damage type propogates to internal
    [DataField("surfacePenMod")] public float SurfacePenModifier = 1.0f;

    //used to calculate how much this damage type propogates to structure
    [DataField("internalPenMod")] public float InternalPenModifier = 1.0f;

    //used to calculate how much this damage type propogates to the next layer
    [DataField("structurePenMod")] public float StructurePenModifier = 1.0f;
}
