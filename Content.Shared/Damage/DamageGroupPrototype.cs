using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage
{
    /// <summary>
    /// A Group of <see cref="DamageTypePrototype"/>s .
    /// </summary>
    [Prototype("damageGroup")]
    [Serializable, NetSerializable]
    public class DamageGroupPrototype : IPrototype, ISerializationHooks
    {
        private IPrototypeManager _prototypeManager = default!;

        [DataField("id", required: true)] public string ID { get; } = default!;

        [DataField("damageTypes", required: true)]
        public List<string> TypeIDs { get; } = default!;

        public HashSet<DamageTypePrototype> DamageTypes { get; } = new();


        // Create list of set of damage types
        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var typeID in TypeIDs)
            {
                DamageTypes.Add(_prototypeManager.Index<DamageTypePrototype>(typeID));
            }

        }

    /// <summary>
    /// Convert a dictionary with damage type keys to a dictionary of damage groups keys.
    /// </summary>
    /// <remarks>
    /// Takes a dictionary with damage types as key and integers as values, and an iterable list of damge groups. Returns a
    /// dictionary with damage group keys, with values calculated by adding up the values for each damage type in that
    /// group key. If a damage type is associated with more than one supported damage group, it will contribute to the
    /// total of each group. Conversely, some damage types may not be represented in the new dictionary.
    /// </remarks>
    /// <param name="damageTypeDict"></param>
    /// <returns></returns>
    public static IReadOnlyDictionary<DamageGroupPrototype, int>
            DamageTypeDictToDamageGroupDict(IReadOnlyDictionary<DamageTypePrototype, int> damageTypeDict, IEnumerable<DamageGroupPrototype> groupKeys)
        {
            var damageGroupDict = new Dictionary<DamageGroupPrototype, int>();
            int damageGroupSumDamage, damageTypeDamage;
            foreach (var group in groupKeys)
            {
                // Add for each damageType in this group, add up the damages present in damageTypeDict
                damageGroupSumDamage = 0;
                foreach (var type in group.DamageTypes)
                {
                    // if the damage type is in the dictionary, add it's damage to the group total.
                    if (damageTypeDict.TryGetValue(type, out damageTypeDamage))
                    {
                        damageGroupSumDamage += damageTypeDamage;
                    }
                }
                damageGroupDict.Add(group, damageGroupSumDamage);
            }
            return damageGroupDict;
        }
    }
}
