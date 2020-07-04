using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.DamageSystem {
    public enum DamageClass { Brute, Burn, Toxin, Airloss }
    public enum DamageType { Blunt, Piercing, Heat, Disintegration, Cellular, DNA, Asphyxiation }

    public static class DamageContainerValues
    {
        private static readonly Dictionary<DamageClass, List<DamageType>> ClassToType = new Dictionary<DamageClass, List<DamageType>>
        {
            { DamageClass.Brute, new List<DamageType>{ DamageType.Blunt, DamageType.Piercing }},
            { DamageClass.Burn, new List<DamageType>{ DamageType.Heat, DamageType.Disintegration }},
            { DamageClass.Toxin, new List<DamageType>{ DamageType.Cellular, DamageType.DNA}},
            { DamageClass.Airloss, new List<DamageType>{ DamageType.Asphyxiation }}
        };

        //TODO: autogenerate this lol
        private static readonly Dictionary<DamageType, DamageClass> TypeToClass = new Dictionary<DamageType, DamageClass>
        {
            { DamageType.Blunt, DamageClass.Brute },
            { DamageType.Piercing, DamageClass.Brute },
            { DamageType.Heat, DamageClass.Burn },
            { DamageType.Disintegration, DamageClass.Burn },
            { DamageType.Cellular, DamageClass.Toxin },
            { DamageType.DNA, DamageClass.Toxin },
            { DamageType.Asphyxiation, DamageClass.Airloss }
        };

        public static DamageClass DamageTypeToClass(DamageType t)
        {
            return TypeToClass[t];
        }
        public static List<DamageType> DamageClassToType(DamageClass c)
        {
            return ClassToType[c];
        }

    }
}
