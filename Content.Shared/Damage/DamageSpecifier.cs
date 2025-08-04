using System.Text.Json.Serialization;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    /// <summary>
    ///     This class represents a collection of damage types and damage values.
    /// </summary>
    /// <remarks>
    ///     The actual damage information is stored in <see cref="DamageDict"/>. This class provides
    ///     functions to apply resistance sets and supports basic math operations to modify this dictionary.
    /// </remarks>
    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class DamageSpecifier : IEquatable<DamageSpecifier>
    {
        // These exist solely so the wiki works. Please do not touch them or use them.
        [JsonPropertyName("types")]
        [DataField("types", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, DamageTypePrototype>))]
        [UsedImplicitly]
        private Dictionary<string,FixedPoint2>? _damageTypeDictionary;

        [JsonPropertyName("groups")]
        [DataField("groups", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, DamageGroupPrototype>))]
        [UsedImplicitly]
        private Dictionary<string, FixedPoint2>? _damageGroupDictionary;

        /// <summary>
        ///     Main DamageSpecifier dictionary. Most DamageSpecifier functions exist to somehow modifying this.
        /// </summary>
        [JsonIgnore]
        [ViewVariables(VVAccess.ReadWrite)]
        [IncludeDataField(customTypeSerializer: typeof(DamageSpecifierDictionarySerializer), readOnly: true)]
        public Dictionary<string, FixedPoint2> DamageDict { get; set; } = new();

        /// <summary>
        ///     Returns a sum of the damage values.
        /// </summary>
        /// <remarks>
        ///     Note that this being zero does not mean this damage has no effect. Healing in one type may cancel damage
        ///     in another. Consider using <see cref="AnyPositive"/> or <see cref="Empty"/> instead.
        /// </remarks>
        public FixedPoint2 GetTotal()
        {
            var total = FixedPoint2.Zero;
            foreach (var value in DamageDict.Values)
            {
                total += value;
            }
            return total;
        }

        /// <summary>
        /// Returns true if the specifier contains any positive damage values.
        /// Differs from <see cref="Empty"/> as a damage specifier might contain entries with zeroes.
        /// This also returns false if the specifier only contains negative values.
        /// </summary>
        public bool AnyPositive()
        {
            foreach (var value in DamageDict.Values)
            {
                if (value > FixedPoint2.Zero)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Whether this damage specifier has any entries.
        /// </summary>
        [JsonIgnore]
        public bool Empty => DamageDict.Count == 0;

        #region constructors
        /// <summary>
        ///     Constructor that just results in an empty dictionary.
        /// </summary>
        public DamageSpecifier() { }

        /// <summary>
        ///     Constructor that takes another DamageSpecifier instance and copies it.
        /// </summary>
        public DamageSpecifier(DamageSpecifier damageSpec)
        {
            DamageDict = new(damageSpec.DamageDict);
        }

        /// <summary>
        ///     Constructor that takes a single damage type prototype and a damage value.
        /// </summary>
        public DamageSpecifier(DamageTypePrototype type, FixedPoint2 value)
        {
            DamageDict = new() { { type.ID, value } };
        }

        /// <summary>
        ///     Constructor that takes a single damage group prototype and a damage value. The value is divided between members of the damage group.
        /// </summary>
        public DamageSpecifier(DamageGroupPrototype group, FixedPoint2 value)
        {
            // Simply distribute evenly (except for rounding).
            // We do this by reducing remaining the # of types and damage every loop.
            var remainingTypes = group.DamageTypes.Count;
            var remainingDamage = value;
            foreach (var damageType in group.DamageTypes)
            {
                var damage = remainingDamage / FixedPoint2.New(remainingTypes);
                DamageDict.Add(damageType, damage);
                remainingDamage -= damage;
                remainingTypes -= 1;
            }
        }
        #endregion constructors

        /// <summary>
        ///     Reduce (or increase) damages by applying a damage modifier set.
        /// </summary>
        /// <remarks>
        ///     Only applies resistance to a damage type if it is dealing damage, not healing.
        ///     This will never convert damage into healing.
        /// </remarks>
        public static DamageSpecifier ApplyModifierSet(DamageSpecifier damageSpec, DamageModifierSet modifierSet)
        {
            // Make a copy of the given data. Don't modify the one passed to this function. I did this before, and weapons became
            // duller as you hit walls. Neat, but not FixedPoint2ended. And confusing, when you realize your fists don't work no
            // more cause they're just bloody stumps.
            DamageSpecifier newDamage = new();
            newDamage.DamageDict.EnsureCapacity(damageSpec.DamageDict.Count);

            foreach (var (key, value) in damageSpec.DamageDict)
            {
                if (value == 0)
                    continue;

                if (value < 0)
                {
                    newDamage.DamageDict[key] = value;
                    continue;
                }

                float newValue = value.Float();

                if (modifierSet.FlatReduction.TryGetValue(key, out var reduction))
                    newValue = Math.Max(0f, newValue - reduction); // flat reductions can't heal you

                if (modifierSet.Coefficients.TryGetValue(key, out var coefficient))
                    newValue *= coefficient; // coefficients can heal you, e.g. cauterizing bleeding

                if(newValue != 0)
                    newDamage.DamageDict[key] = FixedPoint2.New(newValue);
            }

            return newDamage;
        }

        /// <summary>
        ///     Reduce (or increase) damages by applying multiple modifier sets.
        /// </summary>
        /// <param name="damageSpec"></param>
        /// <param name="modifierSets"></param>
        /// <returns></returns>
        public static DamageSpecifier ApplyModifierSets(DamageSpecifier damageSpec, IEnumerable<DamageModifierSet> modifierSets)
        {
            bool any = false;
            DamageSpecifier newDamage = damageSpec;
            foreach (var set in modifierSets)
            {
                // This creates a new damageSpec for each modifier when we really onlt need to create one.
                // This is quite inefficient, but hopefully this shouldn't ever be called frequently.
                newDamage = ApplyModifierSet(newDamage, set);
                any = true;
            }

            if (!any)
                newDamage = new DamageSpecifier(damageSpec);

            return newDamage;
        }

        /// <summary>
        ///     Remove any damage entries with zero damage.
        /// </summary>
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

        /// <summary>
        ///     Clamps each damage value to be within the given range.
        /// </summary>
        public void Clamp(FixedPoint2 minValue, FixedPoint2 maxValue)
        {
            DebugTools.Assert(minValue < maxValue);
            ClampMax(maxValue);
            ClampMin(minValue);
        }

        /// <summary>
        ///     Sets all damage values to be at least as large as the given number.
        /// </summary>
        /// <remarks>
        ///     Note that this only acts on damage types present in the dictionary. It will not add new damage types.
        /// </remarks>
        public void ClampMin(FixedPoint2 minValue)
        {
            foreach (var (key, value) in DamageDict)
            {
                if (value < minValue)
                {
                    DamageDict[key] = minValue;
                }
            }
        }

        /// <summary>
        ///     Sets all damage values to be at most some number. Note that if a damage type is not present in the
        ///     dictionary, these will not be added.
        /// </summary>
        public void ClampMax(FixedPoint2 maxValue)
        {
            foreach (var (key, value) in DamageDict)
            {
                if (value > maxValue)
                {
                    DamageDict[key] = maxValue;
                }
            }
        }

        /// <summary>
        ///     This adds the damage values of some other <see cref="DamageSpecifier"/> to the current one without
        ///     adding any new damage types.
        /// </summary>
        /// <remarks>
        ///     This is used for <see cref="DamageableComponent"/>s, such that only "supported" damage types are
        ///     actually added to the component. In most other instances, you can just use the addition operator.
        /// </remarks>
        public void ExclusiveAdd(DamageSpecifier other)
        {
            foreach (var (type, value) in other.DamageDict)
            {
                // CollectionsMarshal my beloved.
                if (DamageDict.TryGetValue(type, out var existing))
                {
                    DamageDict[type] = existing + value;
                }
            }
        }

        /// <summary>
        ///     Add up all the damage values for damage types that are members of a given group.
        /// </summary>
        /// <remarks>
        ///     If no members of the group are included in this specifier, returns false.
        /// </remarks>
        public bool TryGetDamageInGroup(DamageGroupPrototype group, out FixedPoint2 total)
        {
            bool containsMemeber = false;
            total = FixedPoint2.Zero;

            foreach (var type in group.DamageTypes)
            {
                if (DamageDict.TryGetValue(type, out var value))
                {
                    total += value;
                    containsMemeber = true;
                }
            }
            return containsMemeber;
        }

        /// <summary>
        ///     Returns a dictionary using <see cref="DamageGroupPrototype.ID"/> keys, with values calculated by adding
        ///     up the values for each damage type in that group
        /// </summary>
        /// <remarks>
        ///     If a damage type is associated with more than one supported damage group, it will contribute to the
        ///     total of each group. If no members of a group are present in this <see cref="DamageSpecifier"/>, the
        ///     group is not included in the resulting dictionary.
        /// </remarks>
        public Dictionary<string, FixedPoint2> GetDamagePerGroup(IPrototypeManager protoManager)
        {
            var dict = new Dictionary<string, FixedPoint2>();
            GetDamagePerGroup(protoManager, dict);
            return dict;
        }

        /// <inheritdoc cref="GetDamagePerGroup(Robust.Shared.Prototypes.IPrototypeManager)"/>
        public void GetDamagePerGroup(IPrototypeManager protoManager, Dictionary<string, FixedPoint2> dict)
        {
            dict.Clear();
            foreach (var group in protoManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                if (TryGetDamageInGroup(group, out var value))
                    dict.Add(group.ID, value);
            }
        }

        #region Operators
        public static DamageSpecifier operator *(DamageSpecifier damageSpec, FixedPoint2 factor)
        {
            DamageSpecifier newDamage = new();
            foreach (var entry in damageSpec.DamageDict)
            {
                newDamage.DamageDict.Add(entry.Key, entry.Value * factor);
            }
            return newDamage;
        }

        public static DamageSpecifier operator *(DamageSpecifier damageSpec, float factor)
        {
            DamageSpecifier newDamage = new();
            foreach (var entry in damageSpec.DamageDict)
            {
                newDamage.DamageDict.Add(entry.Key, entry.Value * factor);
            }
            return newDamage;
        }

        public static DamageSpecifier operator /(DamageSpecifier damageSpec, FixedPoint2 factor)
        {
            DamageSpecifier newDamage = new();
            foreach (var entry in damageSpec.DamageDict)
            {
                newDamage.DamageDict.Add(entry.Key, entry.Value / factor);
            }
            return newDamage;
        }

        public static DamageSpecifier operator /(DamageSpecifier damageSpec, float factor)
        {
            DamageSpecifier newDamage = new();

            foreach (var entry in damageSpec.DamageDict)
            {
                newDamage.DamageDict.Add(entry.Key, entry.Value / factor);
            }
            return newDamage;
        }

        public static DamageSpecifier operator +(DamageSpecifier damageSpecA, DamageSpecifier damageSpecB)
        {
            // Copy existing dictionary from dataA
            DamageSpecifier newDamage = new(damageSpecA);

            // Then just add types in B
            foreach (var entry in damageSpecB.DamageDict)
            {
                if (!newDamage.DamageDict.TryAdd(entry.Key, entry.Value))
                {
                    // Key already exists, add values
                    newDamage.DamageDict[entry.Key] += entry.Value;
                }
            }
            return newDamage;
        }

        // Here we define the subtraction operator explicitly, rather than implicitly via something like X + (-1 * Y).
        // This is faster because FixedPoint2 multiplication is somewhat involved.
        public static DamageSpecifier operator -(DamageSpecifier damageSpecA, DamageSpecifier damageSpecB)
        {
            DamageSpecifier newDamage = new(damageSpecA);

            foreach (var entry in damageSpecB.DamageDict)
            {
                if (!newDamage.DamageDict.TryAdd(entry.Key, -entry.Value))
                {
                    newDamage.DamageDict[entry.Key] -= entry.Value;
                }
            }
            return newDamage;
        }

        public static DamageSpecifier operator +(DamageSpecifier damageSpec) => damageSpec;

        public static DamageSpecifier operator -(DamageSpecifier damageSpec) => damageSpec * -1;

        public static DamageSpecifier operator *(float factor, DamageSpecifier damageSpec) => damageSpec * factor;

        public static DamageSpecifier operator *(FixedPoint2 factor, DamageSpecifier damageSpec) => damageSpec * factor;

        public bool Equals(DamageSpecifier? other)
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

        public FixedPoint2 this[string key] => DamageDict[key];
    }
    #endregion
}
