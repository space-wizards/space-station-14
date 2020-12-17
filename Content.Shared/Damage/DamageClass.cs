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
        Airloss,
        Genetic
    }

    public static class DamageClassExtensions
    {
        // TODO DAMAGE This but not hardcoded
        private static readonly ImmutableDictionary<DamageClass, List<DamageType>> ClassToType =
            new Dictionary<DamageClass, List<DamageType>>
            {
                {DamageClass.Brute, new List<DamageType> {DamageType.Blunt, DamageType.Slash, DamageType.Piercing}},
                {DamageClass.Burn, new List<DamageType> {DamageType.Heat, DamageType.Shock, DamageType.Cold}},
                {DamageClass.Toxin, new List<DamageType> {DamageType.Poison, DamageType.Radiation}},
                {DamageClass.Airloss, new List<DamageType> {DamageType.Asphyxiation, DamageType.Bloodloss}},
                {DamageClass.Genetic, new List<DamageType> {DamageType.Cellular}}
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
