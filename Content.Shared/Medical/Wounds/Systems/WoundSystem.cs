using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private Dictionary<string, List<string>> _woundPrototypesIdCache = new();
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
            var specifier = GetWoundPrototypeLookupId(woundProto.DamageToApply);
            _woundPrototypesIdCache.TryAdd(specifier, new List<string>());
            _woundPrototypesIdCache[specifier].Add(woundProto.ID);
        }
    }
    private string GetWoundPrototypeLookupId(DamageSpecifier damageSpecifier)
    {
        var temp = "";
        foreach (var damageType in damageSpecifier.DamageDict.Keys)
        {
            temp += damageType;
        }
        return temp;
    }

    public bool TryGetWoundPrototype(DamageSpecifier damage, out WoundPrototype? prototype)
    {

        prototype = null;
        var lookupId = GetWoundPrototypeLookupId(damage);
        if (!_woundPrototypesIdCache.TryGetValue(lookupId, out var woundProtos))
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
