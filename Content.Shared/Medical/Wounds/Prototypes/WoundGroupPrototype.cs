using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Prototypes;

[Prototype("woundGroup")]
public sealed class WoundGroupPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    [DataField("damageType", required: true)]
    public DamageTypePrototype DamageType { get; init; } = default!;

    [DataField("depthModifier", required: false)]
    public float DepthModifier { get; init; } = 1.0f;

    //--- wound prototypes ordered in severity based on the initial ---
    //surface wounds are wounds on skin or exposed bodyparts
    [DataField("SurfaceWounds", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
    public HashSet<string> SurfaceWoundProtos { get; init; } = new();

    //solid wounds are wounds that get caused when affecting a solid surface/object, such as bones or an exoskeleton
    [DataField("solidWounds", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
    public HashSet<string> SolidWoundProtos { get; init; } = new();

    //internal wounds are wounds that are caused when an injury affects internal soft tissue such as organs or flesh
    [DataField("internalWounds", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
    public HashSet<string> InternalWoundProtos { get; init; } = new();


}
