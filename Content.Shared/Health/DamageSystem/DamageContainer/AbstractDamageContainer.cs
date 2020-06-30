using Content.Shared.DamageSystem;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.DamageSystem
{
    /// <summary>
    ///     Holds the information regarding the various forms of damage an object has taken (i.e. brute, burn, or toxic damage). 
    /// </summary>
    [NetSerializable, Serializable]
    public abstract class AbstractDamageContainer
    {
        [ViewVariables]
        public Dictionary<DamageType, int> _damageList = new Dictionary<DamageType, int>();


        [ViewVariables]
        public virtual List<DamageClass> SupportedDamageClasses { get; }
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

        public AbstractDamageContainer(List<DamageClass> supportedClasses)
        {
            SupportedDamageClasses = supportedClasses;
            SetupDamageContainer();
        }
        public AbstractDamageContainer()
        {
            SetupDamageContainer();
        }
        private void SetupDamageContainer()
        {
            foreach (DamageClass damageClass in SupportedDamageClasses)
            {
                DamageContainerValues.DamageClassToType.TryGetValue(damageClass, out List<DamageType> childrenDamageTypes);
                foreach (DamageType damageType in childrenDamageTypes)
                {
                    _damageList.Add(damageType, 0);
                }
            }
        }



        /// <summary>
        ///     Attempts to grab the damage value for the given <see cref="DamageType"/>. Returns false if the container does not support that type.
        /// </summary>
        public bool TryGetDamageValue(DamageType type, out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }

        /// <summary>
        ///     Attempts to set the damage value for the given <see cref="DamageType"/>. Returns false if the container does not support that type, or if there was an error.
        /// </summary>
        public bool TrySetDamageValue(DamageType type, int newValue)
        {
            if (newValue < 0)
                return false;
            DamageContainerValues.DamageTypeToClass.TryGetValue(type, out DamageClass classType);
            if (SupportedDamageClasses.Contains(classType))
            {
               _damageList[type] = newValue;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Attempts to change the damage value for the given <see cref="DamageType"/> - returns false if the container does not support that type.
        /// </summary>
        public bool TryChangeDamageValue(DamageType type, int delta)
        {
            DamageContainerValues.DamageTypeToClass.TryGetValue(type, out DamageClass classType);
            if (SupportedDamageClasses.Contains(classType))
            {
                _damageList[type] = _damageList[type] + delta;
                return true;
            }
            return false;
        }
    }
}
