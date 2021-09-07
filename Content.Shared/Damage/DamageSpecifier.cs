using Content.Shared.Damage.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.Damage
{
    // TODO DAMAGE UNITS Move this whole class away from, using integers. Also get rid of a lot of the rounding. Just
    // use DamageUnit math operators.

    /// <summary>
    ///     Data class with information on a set of damage to deal/heal.
    /// </summary>
    /// <remarks>
    ///     Automatically unpacks damage groups into types, and provides functions to apply resistance sets.
    ///     Supports basic math operations to modify damage.
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
            get => GetCombinedDamageDictionary();
            set => _damageDict = value;
        }
        private Dictionary<string, int>? _damageDict;

        /// <summary>
        ///     Total damage. Note that this being zero does not mean this damage has no effect. Healing in one type may
        ///     cancel damage in another. For the sum of the absolute damage values, see <see
        ///     cref="TotalAbsoluteDamage"/>.
        /// </summary>
        public int TotalDamage => DamageDict.Values.Sum();

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
        public Dictionary<string, int> GetCombinedDamageDictionary()
        {
            if (_damageDict != null && _damageDict.Count > 0)
            {
                return _damageDict;
            }

            // Add all the damage types by just copying the type dictionary (if it is not null).
            if (_damageTypeDictionary != null)
            {
                _damageDict = new(_damageTypeDictionary);
            }
            else
            {
                _damageDict = new();
            }

            // Then resolve any damage groups and add them
            if (_damageGroupDictionary != null)
            {
                foreach (var entry in _damageGroupDictionary)
                {
                    var damageGroup = IoCManager.Resolve<IPrototypeManager>().Index<DamageGroupPrototype>(entry.Key);

                    // Simply distribute evenly (except for rounding).
                    // We do this by reducing remaining the # of types and damage every loop.
                    var remainingTypes = damageGroup.DamageTypes.Count;
                    var remainingDamage = entry.Value;
                    foreach (var damageType in damageGroup.DamageTypes)
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

            return _damageDict;
        }

        /// <summary>
        ///     Reduce (or increase) damages by applying a resistance set.
        /// </summary>
        /// <remarks>
        ///     Only applies resistance to a damage type if it is dealing damage, not healing.
        /// </remarks>
        public static DamageSpecifier ApplyResistanceSet(DamageSpecifier damageSpec, ResistanceSetPrototype resistanceSet)
        {
            // Make a copy of the given data. Don't modify the one passed to this function. I did this before, and weapons became
            // duller as you hit walls. Neat, but not intended. And confusing, when you realize your fists don't work no
            // more cause they're just bloody stumps.
            DamageSpecifier newDamage = new(damageSpec);

            foreach (var entry in newDamage.DamageDict)
            {
                if (entry.Value <= 0) continue;

                float newValue = entry.Value;

                if (resistanceSet.FlatReduction.TryGetValue(entry.Key, out var reduction))
                {
                    newValue -= reduction; 
                    if (newValue <= 0)
                    {
                        // flat reductions cannot heal you
                        newDamage.DamageDict[entry.Key] = 0;
                        continue;
                    }
                }

                if (resistanceSet.Coefficients.TryGetValue(entry.Key, out var coefficient))
                {
                    // negative coefficients **can** heal you.
                    newValue = MathF.Round(newValue*coefficient, MidpointRounding.AwayFromZero);
                }

                newDamage.DamageDict[entry.Key] = (int) newValue;
            }
            return newDamage;
        }

        /// <summary>
        ///     Sum of the absolute value of every damage type. Useful for testing whether resistances have reduced
        ///     damage to zero. Compare with <see cref="TotalDamage"/>, which might be zero despite non-zero damage
        ///     values.
        /// </summary>
        public int TotalAbsoluteDamage()
        {
            var sum = 0;
            foreach (var value in DamageDict.Values)
            {
                sum += Math.Abs(value);
            }
            return sum;
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
