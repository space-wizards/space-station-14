using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed partial class WoundSystem
{
    private const float HealingTickRate = 1f;
    private float _healingTimer;

    private void UpdateHealing(float frameTime)
    {
        _healingTimer += frameTime;
        if (_healingTimer < 3f)
            return;
        _healingTimer -= HealingTickRate;

        foreach (var wound in EntityQuery<WoundComponent>())
        {
            HealWound(wound);
        }
    }

    private void HealWound(WoundComponent wound)
    {
        if (wound.Parent == EntityUid.Invalid || wound.BaseHealingRate + wound.HealingModifier == 0)
            return; //if the wound doesn't heal, do nothing
        //we want to decrease severity so we need to invert the healing rate to become the severity delta.
        var severityDecrease =  -(wound.BaseHealingRate + wound.HealingMultiplier * wound.HealingModifier);
        AddWoundSeverity(wound.Parent, wound.Owner, severityDecrease,null , wound);
        if (wound.SeverityPercentage <= 0.0f)
            FullyHealWound(wound.Parent, wound.Owner, null, wound);
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
