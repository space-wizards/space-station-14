using Content.Shared.Damage.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.Damage
{
    // TODO DAMAGE UNITS Move this whole class away from, using integers. Also get rid of a lot of the rounding. Just
    // use DamageUnit math operators.

    /// <summary>
    ///     This class represents a collection of damage types and damage values.
    /// </summary>
    /// <remarks>
    ///     The actual damage information is stored in <see cref="DamageDict"/>. This class provides
    ///     functions to apply resistance sets and supports basic math operations to modify this dictionary.
    /// </remarks>
    [DataDefinition]
    public class DamageSpecifier
    {
        [DataField("types", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, DamageTypePrototype>))]
        private readonly Dictionary<string,int>? _damageTypeDictionary;

        [DataField("groups", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, DamageGroupPrototype>))]
        private readonly Dictionary<string, int>? _damageGroupDictionary;

        /// <summary>
        ///     Main DamageSpecifier dictionary. Most DamageSpecifier functions exist to somehow modifying this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<string, int> DamageDict
        {
            get
            {
                if (_damageDict == null)
                    DeserializeDamage();
                return _damageDict!;
            }
            set => _damageDict = value;
        }
        private Dictionary<string, int>? _damageDict;

        /// <summary>
        ///     Sum of the damage values.
        /// </summary>
        /// <remarks>
        ///     Note that this being zero does not mean this damage has no effect. Healing in one type may cancel damage
        ///     in another. For this purpose, you should instead use <see cref="TrimZeros()"/> and then check the <see
        ///     cref="Empty"/> property.
        /// </remarks>
        public int Total => DamageDict.Values.Sum();

        /// <summary>
        ///     Whether this damage specifier has any entries.
        /// </summary>
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
        public DamageSpecifier(DamageTypePrototype type, int value)
        {
            DamageDict = new() { { type.ID, value } };
        }

        /// <summary>
        ///     Constructor that takes a single damage group prototype and a damage value. The value is divided between members of the damage group.
        /// </summary>
        public DamageSpecifier(DamageGroupPrototype group, int value)
        {
            _damageGroupDictionary = new() { { group.ID, value } };
        }
        #endregion constructors

        /// <summary>
        ///     Combines the damage group and type datafield dictionaries into a single damage dictionary.
        /// </summary>
        public void DeserializeDamage()
        {
            // Add all the damage types by just copying the type dictionary (if it is not null).
            if (_damageTypeDictionary != null)
            {
                _damageDict = new(_damageTypeDictionary);
            }
            else
            {
                _damageDict = new();
            }

            if (_damageGroupDictionary == null)
                return;

            // Then resolve damage groups and add them
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (var entry in _damageGroupDictionary)
            {
                if (!prototypeManager.TryIndex<DamageGroupPrototype>(entry.Key, out var group))
                {
                    // This can happen if deserialized before prototypes are loaded.
                    Logger.Error($"Unknown damage group given to DamageSpecifier: {entry.Key}");
                    continue;
                }

                // Simply distribute evenly (except for rounding).
                // We do this by reducing remaining the # of types and damage every loop.
                var remainingTypes = group.DamageTypes.Count;
                var remainingDamage = entry.Value;
                foreach (var damageType in group.DamageTypes)
                {
                    var damage = remainingDamage / remainingTypes;
                    if (!_damageDict.TryAdd(damageType, damage))
                    {
                        // Key already exists, add values
                        _damageDict[damageType] += damage;
                    }
                    remainingDamage -= damage;
                    remainingTypes -= 1;
                }
            }
        }

        /// <summary>
        ///     Reduce (or increase) damages by applying a damage modifier set.
        /// </summary>
        /// <remarks>
        ///     Only applies resistance to a damage type if it is dealing damage, not healing.
        /// </remarks>
        public static DamageSpecifier ApplyModifierSet(DamageSpecifier damageSpec, DamageModifierSet modifierSet)
        {
            // Make a copy of the given data. Don't modify the one passed to this function. I did this before, and weapons became
            // duller as you hit walls. Neat, but not intended. And confusing, when you realize your fists don't work no
            // more cause they're just bloody stumps.
            DamageSpecifier newDamage = new(damageSpec);

            foreach (var entry in newDamage.DamageDict)
            {
                if (entry.Value <= 0) continue;

                float newValue = entry.Value;

                if (modifierSet.FlatReduction.TryGetValue(entry.Key, out var reduction))
                {
                    newValue -= reduction;
                    if (newValue <= 0)
                    {
                        // flat reductions cannot heal you
                        newDamage.DamageDict[entry.Key] = 0;
                        continue;
                    }
                }

                if (modifierSet.Coefficients.TryGetValue(entry.Key, out var coefficient))
                {
                    // negative coefficients **can** heal you.
                    newValue = MathF.Round(newValue*coefficient, MidpointRounding.AwayFromZero);
                }

                newDamage.DamageDict[entry.Key] = (int) newValue;
            }

            newDamage.TrimZeros();
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
            DamageSpecifier newDamage = new(damageSpec);
            foreach (var set in modifierSets)
            {
                // this is probably really inefficient. just don't call this in a hot path I guess.
                newDamage = ApplyModifierSet(newDamage, set);
            }

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
        public void Clamp(int minValue = 0, int maxValue = 0)
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
        public void ClampMin(int minValue = 0)
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
        public void ClampMax(int maxValue = 0)
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
                if (DamageDict.ContainsKey(type))
                {
                    DamageDict[type] += value;
                }
            }
        }

        /// <summary>
        ///     Add up all the damage values for damage types that are members of a given group.
        /// </summary>
        /// <remarks>
        ///     If no members of the group are included in this specifier, returns false.
        /// </remarks>
        public bool TryGetDamageInGroup(DamageGroupPrototype group, out int total)
        {
            bool containsMemeber = false;
            total = 0;

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
        public Dictionary<string, int> GetDamagePerGroup()
        {
            var damageGroupDict = new Dictionary<string, int>();
            foreach (var group in IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<DamageGroupPrototype>())
            {
                if (TryGetDamageInGroup(group, out var value))
                {
                    damageGroupDict.Add(group.ID, value);
                }
            }
            return damageGroupDict;
        }

        #region Operators
        public static DamageSpecifier operator *(DamageSpecifier damageSpec, int factor)
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
                newDamage.DamageDict.Add(entry.Key, (int) MathF.Round(entry.Value * factor, MidpointRounding.AwayFromZero));
            }
            return newDamage;
        }

        public static DamageSpecifier operator /(DamageSpecifier damageSpec, int factor)
        {
            DamageSpecifier newDamage = new();
            foreach (var entry in damageSpec.DamageDict)
            {
                newDamage.DamageDict.Add(entry.Key, (int) MathF.Round(entry.Value /  (float) factor, MidpointRounding.AwayFromZero));
            }
            return newDamage;
        }

        public static DamageSpecifier operator /(DamageSpecifier damageSpec, float factor)
        {
            DamageSpecifier newDamage = new();

            foreach (var entry in damageSpec.DamageDict)
            {
                newDamage.DamageDict.Add(entry.Key, (int) MathF.Round(entry.Value / factor, MidpointRounding.AwayFromZero));
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

        public static DamageSpecifier operator -(DamageSpecifier damageSpecA, DamageSpecifier damageSpecB) => damageSpecA + -damageSpecB;

        public static DamageSpecifier operator +(DamageSpecifier damageSpec) => damageSpec;

        public static DamageSpecifier operator -(DamageSpecifier damageSpec) => damageSpec * -1;

        public static DamageSpecifier operator *(float factor, DamageSpecifier damageSpec) => damageSpec * factor;

        public static DamageSpecifier operator *(int factor, DamageSpecifier damageSpec) => damageSpec * factor;
    }
    #endregion
}
