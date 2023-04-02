using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Prototypes;
using Content.Shared.Medical.Treatments.Systems;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem), typeof(TreatmentSystem))]
public sealed class WoundComponent : Component
{
    //what wound should be created if this wound is healed normally?
    [DataField("scarWound", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ScarWound;

    [DataField("healthDamage")] public FixedPoint2 HealthCapDamage;

    [DataField("integrityDamage")] public FixedPoint2 IntegrityDamage;

    [DataField("severityPercentage")] public FixedPoint2 Severity;

    //How many severity points per woundTick does this part heal passively
    [DataField("baseHealingRate")] public FixedPoint2 BaseHealingRate;

    //How many severity points per woundTick does this part heal ontop of the base rate
    [DataField("healingModifier")] public FixedPoint2 HealingModifier;

    //How much to multiply the Healing modifier
    [DataField("healingMultiplier")] public FixedPoint2 HealingMultiplier;

    //Is this wound actively bleeding?
    [DataField("canBleed")] public bool CanBleed;

    [DataField("validTreatments" , customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TreatmentTypePrototype>))]
    public HashSet<string> ValidTreatments = new();
}

[Serializable, NetSerializable]
public sealed class WoundComponentState : ComponentState
{
    public string? ScarWound;
    public FixedPoint2 HealthCapDamage;
    public FixedPoint2 IntegrityDamage;
    public FixedPoint2 Severity;
    public FixedPoint2 BaseHealingRate;
    public FixedPoint2 HealingModifier;
    public FixedPoint2 HealingMultiplier;
    public HashSet<string> ValidTreatments;
    public bool CanBleed;

    public WoundComponentState(string? scarWound, FixedPoint2 healthCapDamage,
        FixedPoint2 integrityDamage, FixedPoint2 severity, FixedPoint2 baseHealingRate, FixedPoint2 healingModifier,
        FixedPoint2 healingMultiplier, bool canBleed,  HashSet<string> validTreatments)
    {
        ScarWound = scarWound;
        HealthCapDamage = healthCapDamage;
        IntegrityDamage = integrityDamage;
        Severity = severity;
        BaseHealingRate = baseHealingRate;
        HealingModifier = healingModifier;
        HealingMultiplier = healingMultiplier;
        CanBleed = canBleed;
        ValidTreatments = validTreatments;
    }
}
