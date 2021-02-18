#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Serializable, NetSerializable]
    public enum DamageClass
    {
        Brute,
        Burn,
        Toxin,
        Airloss,
        Genetic
    }

    public static class DamageClassExtensions
    {
        public static ImmutableList<DamageType> ToTypes(this DamageClass @class)
        {
            return DamageSystem.ClassToType[@class];
        }

        public static Dictionary<DamageClass, T> ToNewDictionary<T>() where T : struct
        {
            return Enum.GetValues(typeof(DamageClass))
                .Cast<DamageClass>()
                .ToDictionary(@class => @class, _ => default(T));
        }

        public static Dictionary<DamageClass, int> ToNewDictionary()
        {
            return ToNewDictionary<int>();
        }
    }
}
