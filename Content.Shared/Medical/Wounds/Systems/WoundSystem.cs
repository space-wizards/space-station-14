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
            success = TryApplyWoundsSkin(target, coveringComp, damage, woundContainer, primaryCovering.Hardened);
        }
        if ( EntityManager.TryGetComponent<BodyPartComponent>(target, out var bodyPart))
        {
            var newDamage = DamageSpecifier.ApplyModifierSet(damage, bodyPart.DamageResistance);
            success |= TryApplyWoundsBodyPart(target, bodyPart, newDamage, woundContainer);
        }
        if (EntityManager.TryGetComponent<OrganComponent>(target, out var organ))
        {
            DamageSpecifier.ApplyModifierSet(damage, organ.DamageResistance);
            success |= TryApplyWoundsOrgan(target, organ, damage, woundContainer);
        }
        return success;
    }
    private bool TryApplyWoundsSkin(EntityUid target, BodySkinComponent skin, DamageSpecifier damage,
        WoundableComponent woundContainer, bool hardenedCovering)
    {
        if (hardenedCovering)
        {

        }
        foreach (var DamageData in damage.GetDamagePerGroup())
        {

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
        switch (woundType)
        {
            case WoundType.Skin:
            {
                return  Math.Clamp(damage/woundableContainer.WoundData[damageType].SkinDamageCap, 0f, 1.0f)  ;
            }
            case WoundType.Internal:
            {
                return  Math.Clamp(damage/woundableContainer.WoundData[damageType].InternalDamageCap, 0f, 1.0f)  ;
            }
            case WoundType.Solid:
            {
                return  Math.Clamp(damage/woundableContainer.WoundData[damageType].SolidDamageCap, 0f, 1.0f)  ;
            }
            default:
                throw new ArgumentException("WoundType was None!");
        }
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
