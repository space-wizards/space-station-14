using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.DamageSystem {
    public enum DamageClass { Brute, Burn, Toxin, Airloss }
    public enum DamageType { Blunt, Piercing, Heat, Disintegration, Cellular, DNA, Asphyxiation }

    public static class DamageContainerValues
    {
        public static readonly Dictionary<DamageClass, List<DamageType>> DamageClassToType = new Dictionary<DamageClass, List<DamageType>>
        {
            { DamageClass.Brute, new List<DamageType>{ DamageType.Blunt, DamageType.Piercing }},
            { DamageClass.Burn, new List<DamageType>{ DamageType.Heat, DamageType.Disintegration }},
            { DamageClass.Toxin, new List<DamageType>{ DamageType.Cellular, DamageType.DNA}},
            { DamageClass.Airloss, new List<DamageType>{ DamageType.Asphyxiation }}
        };

        //TODO: autogenerate this lol
        public static readonly Dictionary<DamageType, DamageClass> DamageTypeToClass = new Dictionary<DamageType, DamageClass>
        {
            { DamageType.Blunt, DamageClass.Brute },
            { DamageType.Piercing, DamageClass.Brute },
            { DamageType.Heat, DamageClass.Burn },
            { DamageType.Disintegration, DamageClass.Burn },
            { DamageType.Cellular, DamageClass.Toxin },
            { DamageType.DNA, DamageClass.Toxin },
            { DamageType.Asphyxiation, DamageClass.Airloss }
        };
    }
}
