using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Components;

[Serializable, NetSerializable]
public sealed class WoundComponentState : ComponentState
{
    public EntityUid Parent;
    public string? ScarWound;
    public FixedPoint2 HealthCapDamage;
    public FixedPoint2 IntegrityDamage;
    public FixedPoint2 Severity;
    public FixedPoint2 BaseHealingRate;
    public FixedPoint2 HealingModifier;
    public FixedPoint2 HealingMultiplier;

    public WoundComponentState(EntityUid parent, string? scarWound, FixedPoint2 healthCapDamage, FixedPoint2 integrityDamage, FixedPoint2 severity, FixedPoint2 baseHealingRate, FixedPoint2 healingModifier, FixedPoint2 healingMultiplier)
    {
        Parent = parent;
        ScarWound = scarWound;
        HealthCapDamage = healthCapDamage;
        IntegrityDamage = integrityDamage;
        Severity = severity;
        BaseHealingRate = baseHealingRate;
        HealingModifier = healingModifier;
        HealingMultiplier = healingMultiplier;
    }
}
