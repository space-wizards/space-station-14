using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private RobustRandom _random = default!;
    //TODO: Make these CVARS!
    private float _hardenedSkinWoundTypeChance = 0.75f; //Chance for hardened skin to recieve a solid instead of surface wound.
    private float _hardenedSkinSeverityAdjustment = 0.2f; //Decrease in severity if hardened skin receives a surface wound

    public override void Initialize()
    {
    }





    //TODO: return an out woundhandle from this!
    public bool TryApplyWounds(EntityUid target, DamageSpecifier damage)
    {
        var success = false;
        if (!EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer))
            return false;
        if (EntityManager.TryGetComponent<BodySkinComponent>(target, out var coveringComp))
        {
            var primaryCovering = _prototypeManager.Index<BodyCoveringPrototype>(coveringComp.PrimaryBodyCoveringId);
            damage = DamageSpecifier.ApplyModifierSet(damage, primaryCovering.Resistance); //apply skin resistances first!
            //TODO: eventually take into account second skin skin for damage resistance
            damage = DamageSpecifier.ApplyModifierSet(damage, coveringComp.DamageModifier);
            foreach (var damageData in damage.DamageDict)
            {
                success = TryApplyWoundsSkin(target, coveringComp, damageData.Key, damageData.Value.Float(), woundContainer, primaryCovering.Hardened);
            }
        }
        if ( EntityManager.TryGetComponent<BodyPartComponent>(target, out var bodyPart))
        {
            var newDamage = DamageSpecifier.ApplyModifierSet(damage, woundContainer.DamageResistance);
            success |= TryApplyWoundsBodyPart(target, bodyPart, newDamage, woundContainer);
        }
        if (EntityManager.TryGetComponent<OrganComponent>(target, out var organ))
        {
            DamageSpecifier.ApplyModifierSet(damage, woundContainer.DamageResistance);
            success |= TryApplyWoundsOrgan(target, organ, damage, woundContainer);
        }
        return success;
    }
    private bool TryApplyWoundsSkin(EntityUid target, BodySkinComponent skin, string damageType ,float damage,
        WoundableComponent woundContainer, bool hardenedSkin)
    {
        if (!woundContainer.WoundData.TryGetValue(damageType, out var woundData))
            return false;

        var woundSeverity = 0f;
        if (hardenedSkin)
        {
            if (_random.Prob(_hardenedSkinWoundTypeChance))
            {
                woundSeverity = CalculateWoundSeverity(woundContainer, damageType, damage, WoundType.Solid);
            }
            else
            {
                woundSeverity = CalculateWoundSeverity(woundContainer, damageType, damage, WoundType.Skin)*_hardenedSkinSeverityAdjustment;
            }
        }
        return true;
    }

    private bool TryApplyWoundsBodyPart(EntityUid target, BodyPartComponent bodyPart, DamageSpecifier damage, WoundableComponent woundContainer)
    {

        return true;
    }
    private bool TryApplyWoundsOrgan(EntityUid target, OrganComponent organ, DamageSpecifier damage, WoundableComponent woundContainer)
    {

        return true;
    }

    private float CalculateWoundSeverity(WoundableComponent woundableContainer, string damageType, float damage, WoundType woundType)
    {
        var newDamage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(damageType), damage);
        damage = DamageSpecifier.ApplyModifierSet(newDamage, woundableContainer.DamageResistance).DamageDict[damageType].Float();
        return woundType switch
        {
            WoundType.Skin => Math.Clamp(damage / woundableContainer.WoundData[damageType].SkinDamageCap, 0f, 1.0f),
            WoundType.Internal => Math.Clamp(damage / woundableContainer.WoundData[damageType].InternalDamageCap, 0f,
                1.0f),
            WoundType.Solid => Math.Clamp(damage / woundableContainer.WoundData[damageType].SolidDamageCap, 0f, 1.0f),
            WoundType.None => throw new ArgumentException("WoundType was None! This should never happen!"),
            _ => throw new ArgumentException("WoundType was None! This should never happen!")
        };
    }
}

[Serializable, NetSerializable]
[Flags]
public enum WoundType
{
    None = 0,
    Skin = 1,
    Internal = 2,
    Solid = 3
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundData (string WoundId, float Severity, float Tended, float Size, float Infected);
