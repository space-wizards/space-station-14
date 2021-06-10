#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Damage
{
    [UsedImplicitly]
    public class DamageSystem : EntitySystem
    {
        public static ImmutableDictionary<DamageClass, ImmutableList<DamageType>> ClassToType { get; } = DefaultClassToType();

        public static ImmutableDictionary<DamageType, DamageClass> TypeToClass { get; } = DefaultTypeToClass();

        private static ImmutableDictionary<DamageClass, ImmutableList<DamageType>> DefaultClassToType()
        {
            return new Dictionary<DamageClass, ImmutableList<DamageType>>
            {
                [DamageClass.Brute] = new List<DamageType>
                {
                    DamageType.Blunt,
                    DamageType.Slash,
                    DamageType.Piercing
                }.ToImmutableList(),
                [DamageClass.Burn] = new List<DamageType>
                {
                    DamageType.Heat,
                    DamageType.Shock,
                    DamageType.Cold
                }.ToImmutableList(),
                [DamageClass.Toxin] = new List<DamageType>
                {
                    DamageType.Poison,
                    DamageType.Radiation
                }.ToImmutableList(),
                [DamageClass.Airloss] = new List<DamageType>
                {
                    DamageType.Asphyxiation,
                    DamageType.Bloodloss
                }.ToImmutableList(),
                [DamageClass.Genetic] = new List<DamageType>
                {
                    DamageType.Cellular
                }.ToImmutableList()
            }.ToImmutableDictionary();
        }

        private static ImmutableDictionary<DamageType, DamageClass> DefaultTypeToClass()
        {
            return new Dictionary<DamageType, DamageClass>
            {
                {DamageType.Blunt, DamageClass.Brute},
                {DamageType.Slash, DamageClass.Brute},
                {DamageType.Piercing, DamageClass.Brute},
                {DamageType.Heat, DamageClass.Burn},
                {DamageType.Shock, DamageClass.Burn},
                {DamageType.Cold, DamageClass.Burn},
                {DamageType.Poison, DamageClass.Toxin},
                {DamageType.Radiation, DamageClass.Toxin},
                {DamageType.Asphyxiation, DamageClass.Airloss},
                {DamageType.Bloodloss, DamageClass.Airloss},
                {DamageType.Cellular, DamageClass.Genetic}
            }.ToImmutableDictionary();
        }
    }
}
