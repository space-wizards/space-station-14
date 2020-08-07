using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Serializable, NetSerializable]
    public enum DamageClass
    {
        Brute,
        Burn,
        Toxin,
        Airloss
    }

    public static class DamageClassExtensions
    {
        private static readonly ImmutableDictionary<DamageClass, List<DamageType>> ClassToType =
            new Dictionary<DamageClass, List<DamageType>>
            {
                {DamageClass.Brute, new List<DamageType> {DamageType.Blunt, DamageType.Piercing}},
                {DamageClass.Burn, new List<DamageType> {DamageType.Heat, DamageType.Disintegration}},
                {DamageClass.Toxin, new List<DamageType> {DamageType.Cellular, DamageType.DNA}},
                {DamageClass.Airloss, new List<DamageType> {DamageType.Asphyxiation}}
            }.ToImmutableDictionary();

        public static List<DamageType> ToTypes(this DamageClass @class)
        {
            return ClassToType[@class];
        }

        public static Dictionary<DamageClass, int> ToDictionary()
        {
            return Enum.GetValues(typeof(DamageClass))
                .Cast<DamageClass>()
                .ToDictionary(@class => @class, type => 0);
        }
    }
}
