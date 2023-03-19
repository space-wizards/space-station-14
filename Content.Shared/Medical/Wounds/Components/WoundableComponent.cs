using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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
    [DataField("startingHealth")] public FixedPoint2 Health = -1;

    //The maximum health this part can have
    [DataField("health", required: true)] public FixedPoint2 HealthCap;
    //The amount maximum health is decreased by, this is affected by wounds
    [DataField("healthCapDamage")] public FixedPoint2 HealthCapDamage;

    //How much health per woundTick does this part heal passively
    [DataField("baseHealingRate")] public FixedPoint2 BaseHealingRate = 0.1f;

    //How much health per woundTick does this part heal ontop of the base rate
    [DataField("healingModifier")] public FixedPoint2 HealingModifier;

    //How much multiply the Healing modifier
    [DataField("healingMultiplier")] public FixedPoint2 HealingMultiplier = 1.0f;

    //How well is this woundable holding up, when this reaches 0 the entity is destroyed/gibbed!
    [DataField("integrity", required: true)]
    public FixedPoint2 Integrity;

    [DataField("destroyWound", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DestroyWoundId;
}

[Serializable, NetSerializable]
public sealed class WoundableComponentState : ComponentState
{
    public HashSet<string>? AllowedTraumaTypes;
    public TraumaModifierSet? TraumaResistance;
    public TraumaModifierSet? TraumaPenResistance;
    public FixedPoint2 Health;
    public FixedPoint2 HealthCap;
    public FixedPoint2 HealthCapDamage;
    public FixedPoint2 BaseHealingRate;
    public FixedPoint2 HealingModifier;
    public FixedPoint2 HealingMultiplier;
    public FixedPoint2 Integrity;
    public string? DestroyWoundId;

    public WoundableComponentState(
        HashSet<string>? allowedTraumaTypes,
        TraumaModifierSet? traumaResistance,
        TraumaModifierSet? traumaPenResistance,
        FixedPoint2 health,
        FixedPoint2 healthCap,
        FixedPoint2 healthCapDamage,
        FixedPoint2 baseHealingRate,
        FixedPoint2 healingModifier,
        FixedPoint2 healingMultiplier,
        FixedPoint2 integrity,
        string? destroyWoundId)
    {
        AllowedTraumaTypes = allowedTraumaTypes;
        TraumaResistance = traumaResistance;
        TraumaPenResistance = traumaPenResistance;
        Health = health;
        HealthCap = healthCap;
        HealthCapDamage = healthCapDamage;
        BaseHealingRate = baseHealingRate;
        HealingModifier = healingModifier;
        HealingMultiplier = healingMultiplier;
        Integrity = integrity;
        DestroyWoundId = destroyWoundId;
    }
}
