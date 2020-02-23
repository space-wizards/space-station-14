using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.BodySystem
{
    public enum DamageClass { Brute, Burn, Toxin, Airloss }
    public enum DamageType { Blunt, Piercing, Heat, Disintegration, Cellular, DNA, Airloss }

    public static class DamageContainerValues
    {
        public static readonly Dictionary<DamageClass, List<DamageType>> DamageMap = new Dictionary<DamageClass, List<DamageType>>
        {
            { DamageClass.Brute, new List<DamageType>{ DamageType.Blunt, DamageType.Piercing }},
            { DamageClass.Burn, new List<DamageType>{ DamageType.Heat, DamageType.Disintegration }},
            { DamageClass.Toxin, new List<DamageType>{ DamageType.Cellular, DamageType.DNA}},
            { DamageClass.Airloss, new List<DamageType>{ DamageType.Airloss }}
        };
    }

    /// <summary>
    ///     Abstract class for all damage container classes.
    /// </summary>
    [NetSerializable, Serializable]
    public abstract class AbstractDamageContainer
    {
        [ViewVariables]
        abstract public List<DamageClass> SupportedDamageClasses { get; }

        private Dictionary<DamageType, int> _damageList = new Dictionary<DamageType, int>();
        [ViewVariables]
        public Dictionary<DamageType, int> DamageList => _damageList;

        /// <summary>
        ///     Sum of all damages kept on record.
        /// </summary>
       [ViewVariables]
        public int Damage
        {
            get
            {
                return _damageList.Sum(x => x.Value);
            }
        }

        public AbstractDamageContainer()
        {
            foreach(DamageClass damageClass in SupportedDamageClasses){
                DamageContainerValues.DamageMap.TryGetValue(damageClass, out List<DamageType> childrenDamageTypes);
                foreach (DamageType damageType in childrenDamageTypes)
                {
                    _damageList.Add(damageType, 0);
                }
            }
        }

        /// <summary>
        ///     Attempts to grab the damage value for the given DamageType - returns false if the container does not support that type.
        /// </summary>
        public bool TryGetDamageValue(DamageType type, out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }
    }
}
