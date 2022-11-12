using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private RobustRandom _random = default!;
    private Dictionary<string, List<string>> _woundPrototypesIdCache = new();
    //TODO: CVAR these
    private int maxWoundOfTypeCount = 10; //maximum wounds of a single type allowed before the wounds always stack
    private float woundStackChance = 0.5f;
    private float severityModifier = 1.0f;
    private float sizeModifier = 1.0f;
    public override void Initialize()
    {
        CachePrototypes(null);
        _prototypeManager.PrototypesReloaded += CachePrototypes;
    }
    private void CachePrototypes(PrototypesReloadedEventArgs? args)
    {
        _woundPrototypesIdCache.Clear(); //Prevent any hot-reload related fuckery
        foreach (var woundProto in _prototypeManager.EnumeratePrototypes<WoundPrototype>())
        {
            var specifier = GetDamageComboId(woundProto.DamageToApply);
            _woundPrototypesIdCache.TryAdd(specifier, new List<string>());
            _woundPrototypesIdCache[specifier].Add(woundProto.ID);
        }
    }
    private string GetDamageComboId(DamageSpecifier damageSpecifier)
    {
        var temp = "";
        foreach (var damageType in damageSpecifier.DamageDict.Keys)
        {
            temp += damageType;
        }
        return temp;
    }

    public int WoundCount(EntityUid target, string woundProtoId)
    {
        return !EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer) ? 0 : WoundCount(woundContainer, woundProtoId);
    }

    public int WoundCount(WoundableComponent woundContainer, string woundProtoId)
    {
        return woundContainer.Wounds.TryGetValue(woundProtoId, out var woundData) ? woundData.Count : 0;
    }

    //Wound handles are only valid so long as the their their woundData list is not modified.
    public WoundHandle CreateWoundHandle(EntityUid target, string woundProtoId, int woundIndex)
    {
        return !EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer) ? new WoundHandle() : new WoundHandle(woundContainer, woundProtoId, woundIndex);
    }

    //Get a COPY of woundData from a WoundHandle
    public WoundData GetWoundData(WoundHandle handle)
    {
        if (!handle.Valid)
            throw new ArgumentException("Tried to get WoundData from a null handle");
        return handle.Parent.Wounds[handle.Prototype][handle.WoundIndex];
    }
    //Gets a REF of woundData from a WoundHandle, this is only valid until the woundList of that woundtype is modified
    public ref WoundData GetWoundDataRef(WoundHandle handle)
    {
        if (!handle.Valid)
            throw new ArgumentException("Tried to get WoundData from a null handle");
        return ref CollectionsMarshal.AsSpan(handle.Parent.Wounds[handle.Prototype])[handle.WoundIndex];
    }

    private Span<WoundData> GetWoundDataListAsSpan(WoundableComponent woundable, string protoId)
    {
        if (!woundable.Wounds.ContainsKey(protoId))
            throw new ArgumentException("Cannot get WoundData, woundType not found in woundData");
        return CollectionsMarshal.AsSpan(woundable.Wounds[protoId]);
    }

    private bool TryGetWoundDataListAsSpan(WoundableComponent woundable, string protoId,
        out Span<WoundData> woundDataSpan)
    {
        woundDataSpan = null;
        if (!woundable.Wounds.TryGetValue(protoId, out var woundDataList))
            return false;
        woundDataSpan =  CollectionsMarshal.AsSpan(woundDataList);
        return true;
    }

    private WoundHandle TryStackWound(WoundableComponent woundContainer, string woundProtoId, float size, float severity)
    {
        if (!TryGetWoundDataListAsSpan(woundContainer, woundProtoId, out var woundDataSpan))
            throw new ArgumentException("Invalid or non-initialized woundPrototypeId");
        var woundIndex = _random.Next(0, woundDataSpan.Length);
        ref var baseWound = ref woundDataSpan[woundIndex];
        baseWound.Tended = 0; //reopen that wound :D
        //always stack if we exceed the max number of wounds of this type
        if (woundDataSpan.Length < maxWoundOfTypeCount && !_random.Prob(woundStackChance))
            return new WoundHandle(); //return an invalid handle

        var woundHandle = new WoundHandle(woundContainer, woundProtoId, woundIndex);
        var newSeverity = Math.Clamp(severity * severityModifier, 0f, 1f);
        var newSize = Math.Clamp(size * sizeModifier, 0f, 1f);
        if (baseWound.Severity < newSeverity)
        {
            baseWound.Severity = newSeverity;
        }
        if (baseWound.Size < newSize)
        {
            baseWound.Size = newSize;
        }
        return woundHandle;
    }

    private WoundHandle ApplyWound(WoundableComponent woundContainer, string woundProtoId, float size, float severity)
    {
        WoundHandle woundHandle;
        if (woundContainer.Wounds.ContainsKey(woundProtoId))
        {
            woundHandle = TryStackWound(woundContainer, woundProtoId, size, severity);
            if (woundHandle.Valid)
                return woundHandle;
        }
        woundContainer.Wounds[woundProtoId].Add(new WoundData(woundProtoId, severity, 0, size, 0));
        woundHandle = new WoundHandle(woundContainer, woundProtoId, woundContainer.Wounds[woundProtoId].Count-1);
        return woundHandle;
    }

    public bool TryGetWoundPrototype(DamageSpecifier damage, out WoundPrototype? prototype)
    {
        prototype = null;
        var damageComboId = GetDamageComboId(damage);
        if (!_woundPrototypesIdCache.TryGetValue(damageComboId, out var woundProtos))
        {
            return false;
        }
        var valid = false;
        foreach (var woundId in woundProtos)
        {
            valid = true;
            var testProto = _prototypeManager.Index<WoundPrototype>(woundId);
            foreach (var protoDamageDef in testProto.DamageToApply.DamageDict)
            {
                valid = damage.DamageDict[protoDamageDef.Key] >= protoDamageDef.Value;
                if (!valid)
                    break;
            }

            if (valid)
                prototype = testProto;
        }
        return valid;
    }
    public static class A
    {
        // IT LIVES ON! FOREVER IN OUR HEARTS!
    }
}
