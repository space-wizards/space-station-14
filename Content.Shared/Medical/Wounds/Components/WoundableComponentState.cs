using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Components;

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
