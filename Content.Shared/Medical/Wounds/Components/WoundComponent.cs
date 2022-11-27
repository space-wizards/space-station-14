using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundSystem))]
public sealed class WoundComponent : Component
{
    //this is used for caching the parent woundable for use inside and entity query.
    //wounds should NEVER exist without a parent so this will always have a value
    public EntityUid Parent = default;

    [DataField("scarWound", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ScarWound;

    //TODO: implement fixedpoint4
    [DataField("healthDamage")] public FixedPoint2 HealthCapDamage;

    [DataField("integrityDamage")] public FixedPoint2 IntegrityDamage;

    [DataField("severityPercentage")] public float SeverityPercentage = 1.0f;

    //How many severity points per woundTick does this part heal passively
    [DataField("baseHealingRate")] public float BaseHealingRate = 0.05f;

    //How many severity points per woundTick does this part heal ontop of the base rate
    [DataField("healingModifier")] public float HealingModifier;

    //How much to multiply the Healing modifier
    [DataField("healingMultiplier")] public float HealingMultiplier = 1.0f;
}
