using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed partial class WoundSystem
{
    private const float HealingTickRate = 1f;
    private float _healingTimer;

    private void UpdateHealing(float frameTime)
    {
        _healingTimer += frameTime;
        if (_healingTimer < HealingTickRate)
            return;
        _healingTimer -= HealingTickRate;

        foreach (var woundable in EntityQuery<WoundableComponent>())
        {
            foreach (var wound in GetAllWoundComponents(woundable.Owner))
            {
                HealWound(woundable,wound);
            }
            HealWoundable(woundable);
        }
    }

    private void HealWoundable(WoundableComponent woundable)
    {
        var healthCap = woundable.HealthCap - woundable.HealthCapDamage;
        if (woundable.BaseHealingRate + woundable.HealingModifier == 0 || healthCap <= 0)
            return; //if the woundable doesn't heal, do nothing

        var healing = (woundable.BaseHealingRate + woundable.HealingModifier) * woundable.HealingMultiplier;
        if (woundable.Health < healthCap)
            woundable.Health = Math.Clamp(woundable.Health+healing, 0.0f, healthCap);

    }

    private void HealWound(WoundableComponent woundable, WoundComponent wound)
    {
        if (wound.BaseHealingRate + wound.HealingModifier == 0)
            return; //if the wound doesn't heal, do nothing
        //we want to decrease severity so we need to invert the healing rate to become the severity delta.
        var severityDecrease =  -((wound.BaseHealingRate + wound.HealingModifier) * wound.HealingMultiplier);
        AddWoundSeverity(woundable.Owner, wound.Owner, severityDecrease,woundable , wound);
        if (wound.SeverityPercentage <= 0.0f)
            FullyHealWound(woundable.Owner, wound.Owner, woundable, wound);
    }
    public bool FullyHealWound(EntityUid woundableId, EntityUid woundId, WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false))
            return false;
        if (!Resolve(woundId, ref wound, false))
            return false;
        Logger.Log(LogLevel.Info,"Wound "+ woundId + " Fully Healed!");
        return RemoveWound(woundableId, woundId, true, woundable, wound);
    }

    public bool AddHealingModifier (EntityUid woundId, float additionalHealing, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingModifier += additionalHealing;
        return true;
    }

    public bool SetHealingModifier (EntityUid woundId, float newHealingModifier, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingModifier = newHealingModifier;
        return true;
    }

    public bool AddHealingMultipler (EntityUid woundId, float multiplier, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingMultiplier += multiplier;
        return true;
    }

    public bool SetHealingMultiplier (EntityUid woundId, float multiplier, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingMultiplier = multiplier;
        return true;
    }
}
