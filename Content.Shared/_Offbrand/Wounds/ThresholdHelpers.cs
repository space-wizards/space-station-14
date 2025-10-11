using System.Linq;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

public static class ThresholdHelpers
{
    public static TValue? HighestMatch<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey value) where TKey : IComparable<TKey> where TValue : struct
    {
        foreach (var (threshold, data) in dictionary.Reverse())
        {
            if (value.CompareTo(threshold) < 0)
                continue;

            return data;
        }

        return null;
    }

    public static TValue? LowestMatch<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey value) where TKey : IComparable<TKey> where TValue : struct
    {
        foreach (var (threshold, data) in dictionary)
        {
            if (value.CompareTo(threshold) > 0)
                continue;

            return data;
        }

        return null;
    }

    public static FixedPoint2 Count(IEnumerable<ProtoId<DamageTypePrototype>> types, DamageSpecifier specifier)
    {
        var accumulator = FixedPoint2.Zero;

        foreach (var type in types)
        {
            if (specifier.DamageDict.TryGetValue(type, out var amount))
                accumulator += amount;
        }

        return accumulator;
    }

    public static MobState Max(MobState a, MobState b)
    {
        if ((byte)a > (byte)b)
            return a;
        else
            return b;
    }
}
