using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<string, WoundMetaData> _cachedDamageWoundMetaData = new();

    public override void Initialize()
    {
        CacheData(null);
        _prototypeManager.PrototypesReloaded += CacheData;
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageReceived);
    }

    private void OnDamageReceived(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta != null)
            TryApplyWounds(uid, args.DamageDelta);
    }

    private void CacheData(PrototypesReloadedEventArgs? prototypesReloadedEventArgs)
    {
        _cachedDamageWoundMetaData.Clear();
        foreach (var woundGroup in _prototypeManager.EnumeratePrototypes<WoundGroupPrototype>())
        {
            if (!_cachedDamageWoundMetaData.TryAdd(woundGroup.DamageType, new WoundMetaData(woundGroup)))
                Logger.Error("Woundgroup has duplicate damageType! ID:" + woundGroup.ID + " DamageType:" +
                             woundGroup.DamageType);
        }
    }

    private SortedDictionary<float, string>? GetWoundTableForDamageType(string damageTypeId,
        WoundCategory woundCategory)
    {
        if (!_cachedDamageWoundMetaData.ContainsKey(damageTypeId))
            return null;
        return woundCategory switch
        {
            WoundCategory.Surface => _cachedDamageWoundMetaData[damageTypeId].SurfaceWounds,
            WoundCategory.Internal => _cachedDamageWoundMetaData[damageTypeId].InternalWounds,
            WoundCategory.Structural => _cachedDamageWoundMetaData[damageTypeId].StructuralWounds,
            _ => throw new ArgumentException("WoundLayers was invalid! This should never happen!")
        };
    }

    public bool TryGetWoundPen(string damageType, WoundCategory category, out float? woundPen)
    {
        woundPen = null;
        if (!_cachedDamageWoundMetaData.TryGetValue(damageType, out var woundMetaData))
            return false;
        switch (category)
        {
            case WoundCategory.Surface:
            {
                woundPen = woundMetaData.SurfacePenModifier;
                return true;
            }
            case WoundCategory.Internal:
            {
                woundPen = woundMetaData.InternalPenModifier;
                return true;
            }
            case WoundCategory.Structural:
            {
                woundPen = woundMetaData.StructuralPenModifier;
                return true;
            }
            default:
                throw new ArgumentException("Wound category is invalid! This should never happen!");
        }
    }

    public WoundData? CreateWound(string damageType, WoundCategory woundCategory, float level)
    {
        if (level < 0) //if level is invalid return null
            return null;
        var woundTable = GetWoundTableForDamageType(damageType, woundCategory);
        if (woundTable == null)
            return null;
        //wound severity is the percentage to reach the next wound level, it describes how severe this particular wound is
        var woundId = PickWound(level, woundTable, out var severity);
        return new WoundData(woundId, severity, woundCategory, damageType);
    }

    //TODO: output a WoundHandle
    public bool ForceApplyWound(EntityUid target, string woundId, WoundCategory category, float severity)
    {
        if (!EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer))
            return false;
        if (!ContainsForcableWound(woundContainer, woundId))
            return false;
        AddWound(woundContainer, category, new WoundData(woundId, severity, category));
        return true;
    }


    //TODO: output a WoundHandle
    public bool ApplyWound(EntityUid target, WoundData woundData, WoundCategory woundCategory)
    {
        if (!EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer))
            return false;
        //TODO: implement stacking/severity increases later!
        AddWound(woundContainer, woundCategory, woundData);
        return true;
    }

    public bool ContainsForcableWound(WoundableComponent woundableComponent, string woundId)
    {
        //Here be dragons
        return //fuck if statements, we use boolean ors in this neighborhood
            (woundableComponent.ForcedSurfaceWounds != null && woundableComponent.ForcedSurfaceWounds.Contains(woundId))
            ||
            (woundableComponent.ForcedInternalWounds != null &&
             woundableComponent.ForcedInternalWounds.Contains(woundId))
            ||
            (woundableComponent.ForcedStructuralWounds != null &&
             woundableComponent.ForcedStructuralWounds.Contains(woundId));
    }

    public bool CreateAndApplyWound(WoundableComponent woundComponent, string damageType, WoundCategory category,
        float level, out WoundData? newWound)
    {
        newWound = CreateWound(damageType, category, level);
        if (!newWound.HasValue)
            return false;
        AddWound(woundComponent, category, newWound.Value);
        return true;
    }

    public bool CreateAndApplyWound(WoundableComponent woundComponent, string damageType, WoundCategory category,
        float level)
    {
        return CreateAndApplyWound(woundComponent, damageType, category, level, out var _);
    }

    public bool CreateAndApplyWound(EntityUid target, string damageType, WoundCategory category, float level,
        out WoundData? newWound)
    {
        newWound = CreateWound(damageType, category, level);
        return newWound.HasValue && ApplyWound(target, newWound.Value, category);
    }

    public bool CreateAndApplyWound(EntityUid target, string damageType, WoundCategory category, float severity)
    {
        return CreateAndApplyWound(target, damageType, category, severity, out _);
    }


    public bool TryApplyWounds(EntityUid target, DamageSpecifier damage)
    {
        var success = false;
        foreach (var damageData in damage.DamageDict)
        {
            if (damageData.Value > 0)
                success |= TryApplyWounds(target, damageData.Key, damageData.Value);
        }

        return success;
    }

    //try to apply wounds to a part for specific damage
    public bool TryApplyWounds(EntityUid target, string damageType, FixedPoint2 damage)
    {
        var success = false;
        if (!EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer))
            return false;
        if (!_prototypeManager.TryIndex<DamageTypePrototype>(damageType, out var damageTypeProto))
            return false;
        var damageSpec = new DamageSpecifier(damageTypeProto, damage);
        if (woundContainer.DamageResistance != null)
            damageSpec = DamageSpecifier.ApplyModifierSet(damageSpec, woundContainer.DamageResistance);
        if (damageSpec.DamageDict[damageType] == 0)
            return false;
        //TODO: implement custom wounds defined by skins
        //if (EntityManager.TryGetComponent<BodySkinComponent>(target, out var coveringComp))
        if (woundContainer.SurfaceWounds != null)
        {
            damageSpec.DamageDict[damageType] = ApplyLayeringWound(woundContainer, WoundCategory.Surface, damageType,
                damage.Float(), out _);
        }

        if (woundContainer.InternalWounds != null && damageSpec.DamageDict[damageType] > 0)
        {
            damageSpec.DamageDict[damageType] = ApplyLayeringWound(woundContainer, WoundCategory.Internal, damageType,
                damage.Float(), out _);
        }

        if (woundContainer.StructuralWounds == null || damageSpec.DamageDict[damageType] <= 0)
            return success;
        damageSpec.DamageDict[damageType] = ApplyLayeringWound(woundContainer, WoundCategory.Structural, damageType,
            damage.Float(), out var woundLevel);
        if (woundLevel >= 1.0f)
        {
            //TODO: implement structural wound destruction
            //Destroy this if it's a bodypart!
        }

        return success;
    }

    //Apply a layering wound and return and penetrating damage
    private float ApplyLayeringWound(WoundableComponent woundContainer, WoundCategory category, string damageType,
        float damage, out float woundLevel)
    {
        woundLevel = CalculateWoundLevel(woundContainer, WoundCategory.Surface, damageType, damage, out var penDamage);
        if (penDamage != 0 && TryGetWoundPen(damageType, WoundCategory.Surface, out var penMultiplier))
            penDamage *= penMultiplier!.Value; //apply penMultiplier
        CreateAndApplyWound(woundContainer, damageType, WoundCategory.Surface, woundLevel);
        if (penDamage < 0) //this should never be the case but it's good to double check incase someone does a stupid
            penDamage = 0;
        return penDamage;
    }

    private void AddWound(WoundableComponent woundContainer, WoundCategory category, WoundData wound)
    {
        switch (category)
        {
            case WoundCategory.Surface:
                woundContainer.SurfaceWounds?.Add(wound);
                return;
            case WoundCategory.Internal:
                woundContainer.InternalWounds?.Add(wound);
                return;
            case WoundCategory.Structural:
                woundContainer.StructuralWounds?.Add(wound);
                return;
            default:
                throw new ArgumentException("Wound category is invalid! This should never happen!");
        }
    }

    private string PickWound(float level, SortedDictionary<float, string> woundTable, out float severity)
    {
        var nextLevel = 1f;
        var levelFloor = 0f;
        var woundId = string.Empty;
        foreach (var woundLevel in woundTable)
        {
            if (woundLevel.Key > level)
            {
                nextLevel = woundLevel.Key;
                break;
            }

            woundId = woundLevel.Value;
            levelFloor = woundLevel.Key;
        }

        severity = (level - levelFloor) / (nextLevel - level);
        return woundId;
    }

    private float GetDamageCapForDamageType(WoundableComponent woundableComponent, WoundCategory category,
        string damageType)
    {
        switch (category)
        {
            case WoundCategory.Surface:
                if (woundableComponent.SurfaceDamageCap.TryGetValue(damageType, out var cap1))
                    return cap1;
                return -1;
            case WoundCategory.Internal:
                if (woundableComponent.InternalDamageCap.TryGetValue(damageType, out var cap2))
                    return cap2;
                return -1;
            case WoundCategory.Structural:
                if (woundableComponent.StructuralDamageCap.TryGetValue(damageType, out var cap3))
                    return cap3;
                return -1;
            default:
                throw new ArgumentException("Wound category is invalid! This should never happen!");
        }
    }

    //an implementation of calculate wound level that outputs excess damage
    public float CalculateWoundLevel(WoundableComponent woundableComponent, WoundCategory category, string damageType,
        FixedPoint2 damage, out float overDamage)
    {
        var damageCap = GetDamageCapForDamageType(woundableComponent, category, damageType);
        overDamage = 0.0f;
        var level = damage.Float() / damageCap;
        if (!(level > 1.0f))
            return level;
        level = 1.0f;
        overDamage = damage.Float() - damageCap;
        return level;
    }

    //a simpler implementation of calculating wound levels without excess damage output
    public float CalculateWoundLevel(WoundableComponent woundableContainer, WoundCategory category, string damageType,
        FixedPoint2 damage)
    {
        return category switch
        {
            WoundCategory.Surface => Math.Clamp(damage.Float() / woundableContainer.SurfaceDamageCap[damageType], 0f,
                1.0f),
            WoundCategory.Internal => Math.Clamp(damage.Float() / woundableContainer.InternalDamageCap[damageType], 0f,
                1.0f),
            WoundCategory.Structural => Math.Clamp(damage.Float() / woundableContainer.StructuralDamageCap[damageType],
                0f, 1.0f),
            _ => throw new ArgumentException("WoundLayers was invalid! This should never happen!")
        };
    }

    private readonly struct WoundMetaData
    {
        //we do some defensive coding
        public readonly SortedDictionary<float, string>? SurfaceWounds;
        public readonly SortedDictionary<float, string>? InternalWounds;
        public readonly SortedDictionary<float, string>? StructuralWounds;
        public readonly float SurfacePenModifier;
        public readonly float InternalPenModifier;
        public readonly float StructuralPenModifier;

        public WoundMetaData(WoundGroupPrototype damageTypeProto)
        {
            InternalWounds = damageTypeProto.InternalWounds;
            SurfaceWounds = damageTypeProto.SurfaceWounds;
            StructuralWounds = damageTypeProto.StructuralWounds;
            SurfacePenModifier = damageTypeProto.SurfacePenModifier;
            InternalPenModifier = damageTypeProto.InternalPenModifier;
            StructuralPenModifier = damageTypeProto.StructurePenModifier;
        }
    }
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundData(string Id, float Severity, WoundCategory WoundCategory, string? DamageType = null,
    float Tended = 0f, float Infected = 0f)
{
};
