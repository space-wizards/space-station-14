using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed partial class WoundSystem
{
    private const float HealingTickRate = 1f;
    private float _healingTimer;

    #region Public_API

    public void HealAllWounds(EntityUid target, BodyComponent? body = null, WoundableComponent? woundable = null)
    {
        if (Resolve(target, ref woundable))
        {
            foreach (var (woundEntity, wound) in GetAllWounds(target))
            {
                FullyHealWound(target, woundEntity, woundable, wound);
            }
        }

        if (!Resolve(target, ref body))
            return;
        foreach (var (bodyPartEntity, bodyPart) in _body.GetBodyChildren(target, body))
        {
            if (TryComp(bodyPartEntity, out WoundableComponent? woundablePart))
            {
                foreach (var (woundEntity, wound) in GetAllWounds(bodyPartEntity))
                {
                    FullyHealWound(bodyPartEntity, woundEntity, woundablePart, wound);
                }
            }

            foreach (var (organEntity, organ) in _body.GetPartOrgans(bodyPartEntity, bodyPart))
            {
                if (!TryComp(organEntity, out WoundableComponent? woundableOrgan))
                    continue;
                foreach (var (woundId, wound) in GetAllWounds(organEntity))
                {
                    FullyHealWound(bodyPartEntity, woundId, null, wound);
                }
            }
        }
    }

    public void FullyHealWound(EntityUid woundableId, EntityUid woundId, WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable, false) || !Resolve(woundId, ref wound, false))
            return;
        Logger.Log(LogLevel.Info, "Wound " + woundId + " Fully Healed!");
        RemoveWound(woundableId, woundId, true, woundable, wound);
    }

    public bool AddHealingModifier(EntityUid woundId, FixedPoint2 additionalHealing, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingModifier += additionalHealing;
        return true;
    }

    public bool SetHealingModifier(EntityUid woundId, FixedPoint2 newHealingModifier, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingModifier = newHealingModifier;
        return true;
    }

    public bool AddHealingMultipler(EntityUid woundId, FixedPoint2 multiplier, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingMultiplier += multiplier;
        return true;
    }

    public bool SetHealingMultiplier(EntityUid woundId, FixedPoint2 multiplier, WoundComponent? wound = null)
    {
        if (!Resolve(woundId, ref wound, false))
            return false;
        wound.HealingMultiplier = multiplier;
        return true;
    }

    public bool CauterizeWound(EntityUid woundableId, EntityUid woundId,
        WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable) || !Resolve(woundId, ref wound))
            return false;
        return SetWoundCauterize(woundableId, woundId, woundable, wound, true);
    }

    public bool RepenWound(EntityUid woundableId, EntityUid woundId,
        WoundableComponent? woundable = null,
        WoundComponent? wound = null)
    {
        if (!Resolve(woundableId, ref woundable) || !Resolve(woundId, ref wound))
            return false;
        return SetWoundCauterize(woundableId, woundId, woundable, wound, false);
    }

    #endregion

    #region Private_Implementation

    private void UpdateHealing(float frameTime)
    {
        _healingTimer += frameTime;
        if (_healingTimer < HealingTickRate)
            return;
        _healingTimer -= HealingTickRate;

        foreach (var woundable in EntityQuery<WoundableComponent>())
        {
            // TODO wounds before merge iterate wounds separately
            foreach (var wound in GetAllWoundComponents(woundable.Owner))
            {
                HealWound(woundable, wound);
            }

            HealWoundable(woundable);
        }
    }

    private bool SetWoundCauterize(EntityUid woundableId, EntityUid woundId, WoundableComponent woundable,
        WoundComponent wound, bool cauterized)
    {
        if (wound.Cauterized == cauterized)
            return false;
        var oldState = wound.Cauterized;
        wound.Cauterized = cauterized;
        var ev = new WoundCauterizedEvent(woundableId, woundId, woundable, wound, oldState);
        RaiseLocalEvent(woundableId, ref ev, true);
        var bodyId = CompOrNull<BodyPartComponent>(woundableId)?.Body;
        if (!bodyId.HasValue)
            return true;
        //propagate this event to bodyEntity if we are a bodyPart
        var ev2 = new WoundCauterizedEvent(woundableId, woundId, woundable, wound, oldState);
        RaiseLocalEvent(bodyId.Value, ref ev2, true);
        return true;
    }

    private void HealWoundable(WoundableComponent woundable)
    {
        var healthCap = woundable.HealthCap - woundable.HealthCapDamage;
        if (woundable.BaseHealingRate + woundable.HealingModifier == 0 || healthCap <= 0)
            return; //if the woundable doesn't heal, do nothing

        var healing = (woundable.BaseHealingRate + woundable.HealingModifier) * woundable.HealingMultiplier;
        if (woundable.Health < healthCap)
            woundable.Health = FixedPoint2.Clamp(woundable.Health + healing, FixedPoint2.Zero, healthCap);
    }

    private void HealWound(WoundableComponent woundable, WoundComponent wound)
    {
        if (wound.BaseHealingRate + wound.HealingModifier == 0)
            return; //if the wound doesn't heal, do nothing
        //we want to decrease severity so we need to invert the healing rate to become the severity delta.
        var severityDecrease = -((wound.BaseHealingRate + wound.HealingModifier) * wound.HealingMultiplier);
        AddWoundSeverity(woundable.Owner, wound.Owner, severityDecrease, woundable, wound);
        if (wound.Severity <= 0.0f)
            FullyHealWound(woundable.Owner, wound.Owner, woundable, wound);
    }

    #endregion
}
