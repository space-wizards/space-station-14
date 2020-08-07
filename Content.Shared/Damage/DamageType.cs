using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Serializable, NetSerializable]
    public enum DamageType
    {
        Blunt,
        Piercing,
        Heat,
        Disintegration,
        Cellular,
        DNA,
        Asphyxiation
    }

    public static class DamageTypeExtensions
    {
        // TODO: Automatically generate this
        private static readonly ImmutableDictionary<DamageType, DamageClass> TypeToClass =
            new Dictionary<DamageType, DamageClass>
            {
                {DamageType.Blunt, DamageClass.Brute},
                {DamageType.Piercing, DamageClass.Brute},
                {DamageType.Heat, DamageClass.Burn},
                {DamageType.Disintegration, DamageClass.Burn},
                {DamageType.Cellular, DamageClass.Toxin},
                {DamageType.DNA, DamageClass.Toxin},
                {DamageType.Asphyxiation, DamageClass.Airloss}
            }.ToImmutableDictionary();

        public static DamageClass ToClass(this DamageType type)
        {
            return TypeToClass[type];
        }

        public static Dictionary<DamageType, int> ToDictionary()
        {
            return Enum.GetValues(typeof(DamageType))
                .Cast<DamageType>()
                .ToDictionary(type => type, type => 0);
        }

        public static Dictionary<DamageClass, int> ToClassDictionary(IReadOnlyDictionary<DamageType, int> types)
        {
            var classes = DamageClassExtensions.ToDictionary();

            foreach (var @class in classes.Keys.ToList())
            foreach (var type in @class.ToTypes())
            {
                if (!types.TryGetValue(type, out var damage))
                {
                    continue;
                }

                classes[@class] += damage;
            }

            return classes;
        }
    }
}
