using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
[AutoGenerateComponentState]
public sealed class WoundableComponent : Component
{
    [DataField("allowedTraumaTypes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TraumaPrototype>)),
     AutoNetworkedField]
    public HashSet<string>? AllowedTraumaTypes;

    [DataField("traumaResistance"), AutoNetworkedField]
    public TraumaModifierSet? TraumaResistance;

    [DataField("traumaPenResistance"), AutoNetworkedField]
    public TraumaModifierSet? TraumaPenResistance;

    //TODO: implement FixedPoint4
    //How much health does this woundable have, when this reaches 0, it starts taking structural damage
    [DataField("startingHealth"), AutoNetworkedField]
    public FixedPoint2 Health = -1;

    //The maximum health this part can have
    [DataField("health", required: true), AutoNetworkedField]
    public FixedPoint2 HealthCap;

    //The amount maximum health is decreased by, this is affected by wounds
    [DataField("healthCapDamage"), AutoNetworkedField]
    public FixedPoint2 HealthCapDamage;

    //How much health per woundTick does this part heal passively
    [DataField("baseHealingRate"), AutoNetworkedField]
    public FixedPoint2 BaseHealingRate = 0.1f;

    //How much health per woundTick does this part heal ontop of the base rate
    [DataField("healingModifier"), AutoNetworkedField]
    public FixedPoint2 HealingModifier;

    //How much multiply the Healing modifier
    [DataField("healingMultiplier"), AutoNetworkedField]
    public FixedPoint2 HealingMultiplier = 1.0f;

    //How well is this woundable holding up, when this reaches 0 the entity is destroyed/gibbed!
    [DataField("startingIntegrity"), AutoNetworkedField]
    public FixedPoint2 Integrity = -1;

    //The maximum value of integrity that a bodypart may have
    [DataField("integrity", required: true), AutoNetworkedField]
    public FixedPoint2 MaxIntegrity;

    //Wound that is spawned on the part's parent when this part is destroyed
    [DataField("destroyWound", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)),
     AutoNetworkedField]
    public string? DestroyWoundId;
}
