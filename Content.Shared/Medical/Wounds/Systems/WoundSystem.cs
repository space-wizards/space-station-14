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

    private readonly Dictionary<string, CachedWoundTable> _cachedDamageWoundTables = new();

    public override void Initialize()
    {
        CacheData(null);
        _prototypeManager.PrototypesReloaded += CacheData;
    }

    private void CacheData(PrototypesReloadedEventArgs? prototypesReloadedEventArgs)
    {
        _cachedDamageWoundTables.Clear();
        foreach (var damageProto in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
        {
            _cachedDamageWoundTables.Add(damageProto.ID, new CachedWoundTable(damageProto));
        }
    }

    public IReadOnlyDictionary<FixedPoint2, string>? GetWoundTableForDamageType(string damageTypeId, WoundLayer woundLayer)
    {
        if (!_cachedDamageWoundTables.ContainsKey(damageTypeId))
            return null;
        return woundLayer switch
        {
            WoundLayer.Surface => _cachedDamageWoundTables[damageTypeId].SurfaceWounds,
            WoundLayer.Internal => _cachedDamageWoundTables[damageTypeId].InternalWounds,
            WoundLayer.Solid => _cachedDamageWoundTables[damageTypeId].SolidWounds,
            _ => throw new ArgumentException("WoundLayer was invalid! This should never happen!")
        };
    }

    //TODO: output a woundhandle from this!
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
                woundSeverity = CalculateWoundSeverity(woundContainer, damageType, damage, WoundLayer.Solid);
            }
            else
            {
                woundSeverity = CalculateWoundSeverity(woundContainer, damageType, damage, WoundLayer.Surface)*_hardenedSkinSeverityAdjustment;
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

    private float CalculateWoundSeverity(WoundableComponent woundableContainer, string damageType, float damage, WoundLayer woundLayer)
    {
        var newDamage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(damageType), damage);
        damage = DamageSpecifier.ApplyModifierSet(newDamage, woundableContainer.DamageResistance).DamageDict[damageType].Float();
        return woundLayer switch
        {
            WoundLayer.Surface => Math.Clamp(damage / woundableContainer.WoundData[damageType].SkinDamageCap, 0f, 1.0f),
            WoundLayer.Internal => Math.Clamp(damage / woundableContainer.WoundData[damageType].InternalDamageCap, 0f,
                1.0f),
            WoundLayer.Solid => Math.Clamp(damage / woundableContainer.WoundData[damageType].SolidDamageCap, 0f, 1.0f),
            _ => throw new ArgumentException("WoundLayer was invalid! This should never happen!")
        };
    }

    private readonly struct CachedWoundTable
    {
        //we do some defensive coding
        public readonly IReadOnlyDictionary<FixedPoint2, string>? SurfaceWounds;
        public readonly IReadOnlyDictionary<FixedPoint2, string>? InternalWounds;
        public readonly IReadOnlyDictionary<FixedPoint2, string>? SolidWounds;

        public CachedWoundTable(DamageTypePrototype damageTypeProto)
        {
            InternalWounds = damageTypeProto.InternalWounds;
            SurfaceWounds = damageTypeProto.SurfaceWounds;
            SolidWounds = damageTypeProto.SolidWounds;
        }
    }
}

[Serializable, NetSerializable]
[Flags]
public enum WoundLayer
{
    Surface = 0,
    Internal = 1,
    Solid = 2
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundData (string WoundId, float Severity, float Tended, float Size, float Infected);
