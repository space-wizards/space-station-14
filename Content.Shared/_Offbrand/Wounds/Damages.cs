using System.Linq;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.Wounds;

// like DamageSpecifier but with non-fucked network behaviour
[DataDefinition, Serializable, NetSerializable]
public sealed partial class Damages : IEquatable<Damages>, IRobustCloneable<Damages>
{
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> DamageDict;

    public bool Empty => DamageDict.Count == 0;

    public Damages()
    {
        DamageDict = new();
    }

    public Damages(DamageSpecifier origin) : this()
    {
        foreach (var (damage, value) in origin.DamageDict)
        {
            DamageDict[damage] = value;
        }
    }

    public FixedPoint2 GetTotal()
    {
        var total = FixedPoint2.Zero;
        foreach (var value in DamageDict.Values)
        {
            total += value;
        }
        return total;
    }

    public void TrimZeros()
    {
        foreach (var (key, value) in DamageDict)
        {
            if (value == 0)
            {
                DamageDict.Remove(key);
            }
        }
    }

    public DamageSpecifier ToSpecifier()
    {
        var specifier = new DamageSpecifier();
        foreach (var (type, value) in DamageDict)
        {
            specifier.DamageDict[type] = value;
        }
        return specifier;
    }

    public Damages Heal(DamageSpecifier incoming)
    {
        var remainder = new Damages(incoming);

        foreach (var (type, value) in remainder.DamageDict)
        {
            DebugTools.Assert(value <= 0);

            if (!DamageDict.TryGetValue(type, out var existing))
                continue;

            var newValue = existing + value;
            if (newValue <= 0)
            {
                remainder.DamageDict[type] = newValue;
                newValue = 0;
            }
            else
            {
                remainder.DamageDict[type] = 0;
            }

            DamageDict[type] = newValue;
        }

        remainder.TrimZeros();
        return remainder;
    }

    public static Damages operator +(Damages damages, DamageSpecifier specifier)
    {
        var newDamages = damages.Clone();

        foreach (var entry in specifier.DamageDict)
        {
            if (!newDamages.DamageDict.TryAdd(entry.Key, entry.Value))
            {
                newDamages.DamageDict[entry.Key] += entry.Value;
            }
        }

        return newDamages;
    }

    public Damages Clone()
    {
        return new() { DamageDict = new(this.DamageDict) };
    }

    public override string ToString()
    {
        return "Damages(" + string.Join("; ", DamageDict.Select(x => x.Key + ":" + x.Value)) + ")";
    }

    public bool Equals(Damages? other)
    {
        if (other == null || DamageDict.Count != other.DamageDict.Count)
            return false;

        foreach (var (key, value) in DamageDict)
        {
            if (!other.DamageDict.TryGetValue(key, out var otherValue) || value != otherValue)
                return false;
        }

        return true;
    }
}
