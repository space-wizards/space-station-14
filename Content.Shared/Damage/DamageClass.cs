using System.Collections.Generic;
using System.Collections.Immutable;

namespace Content.Shared.Damage
{
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

        public static List<DamageType> ToType(this DamageClass @class)
        {
            return ClassToType[@class];
        }
    }
}
