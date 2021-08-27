using Content.Shared.Damage.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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
    public class DamageSpecifier : ISerializationHooks
    {
        [DataField("types")]
        private Dictionary<string,int>? _damageTypeIdDictionary;

        [DataField("groups")]
        private Dictionary<string, int>? _damageGroupIdDictionary;

        /// <summary>
        ///     Main DamageSpecifier dictionary. Most DamageSpecifier functions exist to somehow modifying this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<DamageTypePrototype, int> DamageDict = new();

        /// <summary>
        ///     Total damage. Note that this being zero does not mean this damage has no effect. Healing in one type may
        ///     cancel damage in another. For the sum of the absolute damage values, see <see
        ///     cref="TotalAbsoluteDamage"/>.
        /// </summary>
        public int TotalDamage => DamageDict.Values.Sum();

        #region Constructors
        /// <summary>
        ///     Constructor that just results in an empty dictionary.
        /// </summary>
        public DamageSpecifier() {}

        /// <summary>
        ///     Constructor that takes a damage type prototype and a single damage value.
        /// </summary>
        public DamageSpecifier(DamageTypePrototype type, int value)
        {
            AddDamageType(type, value);
        }

        /// <summary>
        ///     Constructor that takes a dictionary of damage types.
        /// </summary>
        public DamageSpecifier(Dictionary<DamageTypePrototype, int> dict)
        {
            // Just copy this dictionary
            DamageDict = new(dict);
        }

        /// <summary>
        ///     Constructor that takes another DamageSpecifier instance and copies it.
        /// </summary>
        public DamageSpecifier(DamageSpecifier damageSpec)
        {
            // Just copy the data
            DamageDict = new(damageSpec.DamageDict);
        }

        /// <summary>
        ///     Constructor that takes a damage group prototype and a single damage value. The group is split into
        ///     types, and the damage is distributed between them.
        /// </summary>
        public DamageSpecifier(DamageGroupPrototype group, int value)
        {
            AddDamageGroup(group, value);
        }

        /// <summary>
        ///     Constructor that takes a damage group prototype dictionary. Each group is split into
        ///     types, and has it's damage distributed between those types.
        /// </summary>
        public DamageSpecifier(Dictionary<DamageGroupPrototype, int> dict)
        {
            foreach (var entry in dict)
            {
                AddDamageGroup(entry.Key, entry.Value);
            }
        }
        #endregion

        /// <summary>
        ///     Resolve datafield prototype ID strings
        /// </summary>
        void ISerializationHooks.AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            // use prototypeManager to resolve any damage types
            if (_damageTypeIdDictionary != null)
            {
                foreach (var entry in _damageTypeIdDictionary)
                {
                    AddDamageType(prototypeManager.Index<DamageTypePrototype>(entry.Key), entry.Value);
                }
            }

            // use prototypeManager to resolve any damage groups
            if (_damageGroupIdDictionary != null)
            {
                foreach (var entry in _damageGroupIdDictionary)
                {
                    AddDamageGroup(prototypeManager.Index<DamageGroupPrototype>(entry.Key), entry.Value);
                }
            }

            if (DamageDict.Count == 0)
            {   // Something has gone wrong. Not just the values are zero, but the actual dictionary is empty.
                // This may happen if in a yaml someone gave something a damage data field, but didn't specify either a
                // type or group dictionary (both of which are null-able).
                Logger.Warning("Empty DamageSpecifier dictionary. Bad YAML file?");
            }
        }

        /// <summary>
        ///     Variation of the normal dictionary Add function. If a key already exists, add the damage values
        ///     together.
        /// </summary>
        public void AddDamageType(DamageTypePrototype damageType, int damage)
        {
            if (!DamageDict.TryAdd(damageType, damage))
            {
                DamageDict[damageType] += damage;
            }
        }

        /// <summary>
        ///     Split a damage group and add its types
        /// </summary>
        private void AddDamageGroup(DamageGroupPrototype damageGroup, int damageToDistribute)
        {
            var totalTypes = damageGroup.DamageTypes.Count;

            // Simply distribute evenly (except for rounding).
            // We do this by reducing remaining the # of types and damage every loop.
            foreach (var damageType in damageGroup.DamageTypes)
            {
                AddDamageType(damageType, damageToDistribute/totalTypes);
                damageToDistribute -= damageToDistribute / totalTypes;
                totalTypes -= 1;
            }
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
            DamageSpecifier newDamage = new(damageSpecA.DamageDict);

            // Then just add types in B
            foreach (var entry in damageSpecB.DamageDict)
            {
                newDamage.AddDamageType(entry.Key, entry.Value);
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

