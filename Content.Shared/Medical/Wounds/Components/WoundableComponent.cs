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
public sealed class WoundableComponent : Component
{
    [DataField("allowedTraumaTypes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TraumaPrototype>))]
    public HashSet<string>? AllowedTraumaTypes;

    [DataField("traumaResistance")] public TraumaModifierSet? TraumaResistance;

    [DataField("traumaPenResistance")] public TraumaModifierSet? TraumaPenResistance;

    //TODO: implement FixedPoint4
    //How much health does this woundable have, when this reaches 0, it starts taking structural damage
    [DataField("startingHealth")] public float Health = -1;

    //The maximum health this part can have
    [DataField("health", required: true)] public float HealthCap;
    //The amount maximum health is decreased by, this is affected by wounds
    [DataField("healthCapDamage")] public float HealthCapDamage;

    //How much health per woundTick does this part heal passively
    [DataField("baseHealingRate")] public float BaseHealingRate = 0.1f;

    //How much health per woundTick does this part heal ontop of the base rate
    [DataField("healingModifier")] public float HealingModifier;

    //How much multiply the Healing modifier
    [DataField("healingMultiplier")] public float HealingMultiplier = 1.0f;

    //How well is this woundable holding up, when this reaches 0 the entity is destroyed/gibbed!
    [DataField("integrity", required: true)]
    public FixedPoint2 Integrity;

    [DataField("destroyWound", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DestroyWoundId;
}
