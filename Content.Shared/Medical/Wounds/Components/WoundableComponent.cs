using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent]
public sealed class WoundableComponent : Component
{
    [DataField("surfaceWoundInfo", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
    public Dictionary<string, float> SurfaceDamageCap = new();

    [DataField("internalWoundInfo", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
    public Dictionary<string, float> InternalDamageCap = new();

    [DataField("structuralWoundInfo", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
    public Dictionary<string, float> StructuralDamageCap = new();

    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [ViewVariables, DataField("damageResistance")]
    public DamageModifierSet? DamageResistance = new();

    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [DataField("surfaceWounds")]
    public List<WoundData>? SurfaceWounds;

    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [DataField("internalWounds")]
    public List<WoundData>? InternalWounds;

    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [DataField("structuralWounds")]
    public List<WoundData>? StructuralWounds;

    [Access(typeof(WoundSystem), Other = AccessPermissions.Read)]
    [DataField("forcedSurfaceWounds", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
    public HashSet<string>? ForcedSurfaceWounds; //List of wounds that can be applied regardless of damage type

    [Access(typeof(WoundSystem), Other = AccessPermissions.Read)]
    [DataField("forcedInternalWounds", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
    public HashSet<string>? ForcedInternalWounds; //List of wounds that can be applied regardless of damage type

    [Access(typeof(WoundSystem), Other = AccessPermissions.Read)]
    [DataField("forceStructuralWounds", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<WoundPrototype>))]
    public HashSet<string>? ForcedStructuralWounds; //List of wounds that can be applied regardless of damage type
}

//if a structural wound reaches level 1 and severity 1 the part/skin/bone WILL be lost!
[Serializable, NetSerializable]
public enum WoundCategory
{
    Surface = 1 << 0,
    Internal = 1 << 1,
    Structural = 1 << 2
}
