using System.Text.Json.Serialization;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Shared.chaos
{
    /// <summary>
    ///     This class represents a collection of chaos
    /// </summary>
    /// <remarks>
    ///     This is similar to the DamageSpecifier class.
    /// </remarks>
    [DataDefinition]
    public sealed class ChaosMetrics : IEquatable<ChaosMetrics>
    {
        /// <summary>
        ///     Main chaos dictionary. Most ChaosMetrics functions exist to somehow modifying this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<string, FixedPoint2> ChaosDict { get; set; } = new();

        /// <summary>
        ///     Sum of the chaos values.
        /// </summary>
        [JsonIgnore]
        public FixedPoint2 Total => ChaosDict.Values.Sum();

        /// <summary>
        ///     Whether this chaos specifier has any entries.
        /// </summary>
        [JsonIgnore]
        public bool Empty => ChaosDict.Count == 0;

        #region constructors
        /// <summary>
        ///     Constructor that just results in an empty dictionary.
        /// </summary>
        public ChaosMetrics() { }

        /// <summary>
        ///     Constructor that takes another ChaosMetrics instance and copies it.
        /// </summary>
        public ChaosMetrics(ChaosMetrics chaos)
        {
            ChaosDict = new(chaos.ChaosDict);
        }

        /// <summary>
        ///     Constructor that takes another ChaosMetrics instance and copies it.
        /// </summary>
        public ChaosMetrics(Dictionary<string, FixedPoint2> chaos)
        {
            ChaosDict = new(chaos);
        }
        #endregion
        /// <summary>
        ///     Remove any chaos entries with zero chaos.
        /// </summary>
        public void TrimZeros()
        {
            foreach (var (key, value) in ChaosDict)
            {
                if (value == 0)
                {
                    ChaosDict.Remove(key);
                }
            }
        }

        /// <summary>
        ///     Clamps each chaos value to be within the given range.
        /// </summary>
        public void Clamp(FixedPoint2 minValue, FixedPoint2 maxValue)
        {
            DebugTools.Assert(minValue < maxValue);
            ClampMax(maxValue);
            ClampMin(minValue);
        }

        /// <summary>
        ///     Sets all chaos values to be at least as large as the given number.
        /// </summary>
        /// <remarks>
        ///     Note that this only acts on chaos types present in the dictionary. It will not add new chaos types.
        /// </remarks>
        public void ClampMin(FixedPoint2 minValue)
        {
            foreach (var (key, value) in ChaosDict)
            {
                if (value < minValue)
                {
                    ChaosDict[key] = minValue;
                }
            }
        }

        /// <summary>
        ///     Sets all chaos values to be at most some number. Note that if a chaos type is not present in the
        ///     dictionary, these will not be added.
        /// </summary>
        public void ClampMax(FixedPoint2 maxValue)
        {
            foreach (var (key, value) in ChaosDict)
            {
                if (value > maxValue)
                {
                    ChaosDict[key] = maxValue;
                }
            }
        }

        /// <summary>
        ///     This adds the chaos values of some other <see cref="ChaosMetrics"/> to the current one without
        ///     adding any new chaos types.
        /// </summary>
        /// <remarks>
        ///     This is used for <see cref="DamageableComponent"/>s, such that only "supported" chaos types are
        ///     actually added to the component. In most other instances, you can just use the addition operator.
        /// </remarks>
        public void ExclusiveAdd(ChaosMetrics other)
        {
            foreach (var (type, value) in other.ChaosDict)
            {
                if (ChaosDict.ContainsKey(type))
                {
                    ChaosDict[type] += value;
                }
            }
        }

        #region Operators
        public static ChaosMetrics operator *(ChaosMetrics chaos, FixedPoint2 factor)
        {
            ChaosMetrics newDamage = new();
            foreach (var entry in chaos.ChaosDict)
            {
                newDamage.ChaosDict.Add(entry.Key, entry.Value * factor);
            }
            return newDamage;
        }

        public static ChaosMetrics operator *(ChaosMetrics chaos, float factor)
        {
            ChaosMetrics newDamage = new();
            foreach (var entry in chaos.ChaosDict)
            {
                newDamage.ChaosDict.Add(entry.Key, entry.Value * factor);
            }
            return newDamage;
        }

        public static ChaosMetrics operator /(ChaosMetrics chaos, FixedPoint2 factor)
        {
            ChaosMetrics newDamage = new();
            foreach (var entry in chaos.ChaosDict)
            {
                newDamage.ChaosDict.Add(entry.Key, entry.Value / factor);
            }
            return newDamage;
        }

        public static ChaosMetrics operator /(ChaosMetrics chaos, float factor)
        {
            ChaosMetrics newDamage = new();

            foreach (var entry in chaos.ChaosDict)
            {
                newDamage.ChaosDict.Add(entry.Key, entry.Value / factor);
            }
            return newDamage;
        }

        public static ChaosMetrics operator +(ChaosMetrics chaosA, ChaosMetrics chaosB)
        {
            // Copy existing dictionary from dataA
            ChaosMetrics newDamage = new(chaosA);

            // Then just add types in B
            foreach (var entry in chaosB.ChaosDict)
            {
                if (!newDamage.ChaosDict.TryAdd(entry.Key, entry.Value))
                {
                    // Key already exists, add values
                    newDamage.ChaosDict[entry.Key] += entry.Value;
                }
            }
            return newDamage;
        }

        // Here we define the subtraction operator explicitly, rather than implicitly via something like X + (-1 * Y).
        // This is faster because FixedPoint2 multiplication is somewhat involved.
        public static ChaosMetrics operator -(ChaosMetrics chaosA, ChaosMetrics chaosB)
        {
            ChaosMetrics newDamage = new(chaosA);

            foreach (var entry in chaosB.ChaosDict)
            {
                if (!newDamage.ChaosDict.TryAdd(entry.Key, -entry.Value))
                {
                    newDamage.ChaosDict[entry.Key] -= entry.Value;
                }
            }
            return newDamage;
        }

        public static ChaosMetrics operator +(ChaosMetrics chaos) => chaos;

        public static ChaosMetrics operator -(ChaosMetrics chaos) => chaos * -1;

        public static ChaosMetrics operator *(float factor, ChaosMetrics chaos) => chaos * factor;

        public static ChaosMetrics operator *(FixedPoint2 factor, ChaosMetrics chaos) => chaos * factor;

        public bool Equals(ChaosMetrics? other)
        {
            if (other == null || ChaosDict.Count != other.ChaosDict.Count)
                return false;

            foreach (var (key, value) in ChaosDict)
            {
                if (!other.ChaosDict.TryGetValue(key, out var otherValue) || value != otherValue)
                    return false;
            }

            return true;
        }
        #endregion
    }
}
