using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
[AutoGenerateComponentState]
public sealed class WoundComponent : Component
{
    //what wound should be created if this wound is healed normally?
    [DataField("scarWound", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), AutoNetworkedField]
    public string? ScarWound;

    [DataField("healthDamage"), AutoNetworkedField]
    public FixedPoint2 HealthCapDamage;

    [DataField("integrityDamage"), AutoNetworkedField]
    public FixedPoint2 IntegrityDamage;

    [DataField("severityPercentage"), AutoNetworkedField]
    public FixedPoint2 Severity;

    //How many severity points per woundTick does this part heal passively
    [DataField("baseHealingRate"), AutoNetworkedField]
    public FixedPoint2 BaseHealingRate;

    //How many severity points per woundTick does this part heal ontop of the base rate
    [DataField("healingModifier"), AutoNetworkedField]
    public FixedPoint2 HealingModifier;

    //How much to multiply the Healing modifier
    [DataField("healingMultiplier"), AutoNetworkedField]
    public FixedPoint2 HealingMultiplier;

    //Is this wound actively bleeding?
    [DataField("canBleed"), AutoNetworkedField]
    public bool CanBleed;

    [DataField("validTreatments", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TreatmentTypePrototype>)),
     AutoNetworkedField]
    public HashSet<string> ValidTreatments = new();
}
