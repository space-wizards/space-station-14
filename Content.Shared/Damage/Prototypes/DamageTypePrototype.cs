using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        // --===   Wounding/Medical configuration  ===---

        //Note: these should be defined in order of severity!
        //surface wounds are wounds on skin or exposed bodyparts
        [DataField("surfaceWounds", required: false, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
        public HashSet<WoundPrototype> SurfaceWounds { get; init; } = new();

        //solid wounds are wounds that get caused when affecting a solid surface/object, such as bones or an exoskeleton
        [DataField("solidWounds", required: false, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
        public HashSet<WoundPrototype> SolidWounds { get; init; } = new();

        //internal wounds are wounds that are caused when an injury affects internal soft tissue such as organs or flesh
        [DataField("internalWounds", required: false, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
        public HashSet<WoundPrototype> InternalWounds { get; init; } = new();

        //Modifier for adjusting how much penetration this damage type has to apply internal wounding
        [DataField("penModifier", required: false)]
        public float PenetrationModifier { get; init; } = 1.0f;


    }
}
