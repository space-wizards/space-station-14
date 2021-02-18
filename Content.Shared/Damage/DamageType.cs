using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Serializable, NetSerializable]
    public enum DamageType
    {
        Blunt,
        Slash,
        Piercing,
        Heat,
        Shock,
        Cold,
        Poison,
        Radiation,
        Asphyxiation,
        Bloodloss,
        Cellular
    }

    public static class DamageTypeExtensions
    {
        public static DamageClass ToClass(this DamageType type)
        {
            return DamageSystem.TypeToClass[type];
        }

        public static Dictionary<DamageType, T> ToNewDictionary<T>()
        {
            return Enum.GetValues(typeof(DamageType))
                .Cast<DamageType>()
                .ToDictionary(type => type, _ => default(T));
        }

        public static Dictionary<DamageType, int> ToNewDictionary()
        {
            return ToNewDictionary<int>();
        }

        public static Dictionary<DamageClass, int> ToClassDictionary(IReadOnlyDictionary<DamageType, int> types)
        {
            var classes = DamageClassExtensions.ToNewDictionary();

            foreach (var @class in classes.Keys.ToList())
            {
                foreach (var type in @class.ToTypes())
                {
                    if (!types.TryGetValue(type, out var damage))
                    {
                        continue;
                    }

                    classes[@class] += damage;
                }
            }

            return classes;
        }
    }
}
