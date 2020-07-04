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
    public class DamageContainer
    {
        [ViewVariables]
        public Dictionary<DamageType, int> _damageList = new Dictionary<DamageType, int>();


        [ViewVariables]
        public virtual List<DamageClass> SupportedDamageClasses { get; }

        [ViewVariables]
        public virtual List<DamageType> SupportedDamageTypes {
            get
            {
                List<DamageType> toReturn = new List<DamageType>();
                foreach(DamageClass damageClass in SupportedDamageClasses){
                    toReturn.AddRange(DamageContainerValues.DamageClassToType(damageClass));
                }
                return toReturn;
            }
        }

        public Dictionary<DamageType, int> DamageList => _damageList;


        /// <summary>
        ///     Sum of all damages kept on record.
        /// </summary>
       [ViewVariables]
        public int TotalDamage
        {
            get
            {
                return _damageList.Sum(x => x.Value);
            }
        }

        public DamageContainer(DamageContainerPrototype data)
        {
            SupportedDamageClasses = data.ActiveDamageClasses;
            SetupDamageContainer();
        }
        public DamageContainer(List<DamageClass> supportedClasses)
        {
            SupportedDamageClasses = supportedClasses;
            SetupDamageContainer();
        }
        public DamageContainer()
        {
            SetupDamageContainer();
        }
        private void SetupDamageContainer()
        {
            foreach (DamageClass damageClass in SupportedDamageClasses)
            {
                List<DamageType> childrenDamageTypes = DamageContainerValues.DamageClassToType(damageClass);
                foreach (DamageType damageType in childrenDamageTypes)
                {
                    _damageList.Add(damageType, 0);
                }
            }
        }


        public bool SupportsDamageType(DamageType type)
        {
            return SupportedDamageClasses.Contains(DamageContainerValues.DamageTypeToClass(type));
        }

        /// <summary>
        ///     Attempts to grab the damage value for the given <see cref="DamageType"/>. Returns false if the container does not support that type.
        /// </summary>
        public bool TryGetDamageValue(DamageType type, out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }

        /// <summary>
        ///     Grabs the damage value for the given <see cref="DamageType"/>. Does not check whether the value is supported, and so will throw an error if an invalid type is given.
        /// </summary>
        public int GetDamageValue(DamageType type)
        {
            return _damageList[type];
        }

        /// <summary>
        ///     Attempts to change the damage value for the given <see cref="DamageType"/> - returns false if the container does not support that type.
        /// </summary>
        public bool TryChangeDamageValue(DamageType type, int delta)
        {
            DamageClass classType = DamageContainerValues.DamageTypeToClass(type);
            if (SupportedDamageClasses.Contains(classType))
            {
                _damageList[type] = _damageList[type] + delta;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Changes the damage value for the given <see cref="DamageType"/>. Does not check whether the value is supported, and so will throw an error if an invalid type is given.
        /// </summary>
        public void ChangeDamageValue(DamageType type, int delta)
        {
            _damageList[type] = _damageList[type] + delta;
        }

        /// <summary>
        ///     Attempts to set the damage value for the given <see cref="DamageType"/>. Returns false if the container does not support that type, or if there was an error.
        /// </summary>
        public bool TrySetDamageValue(DamageType type, int newValue)
        {
            if (newValue < 0)
                return false;
            DamageClass classType = DamageContainerValues.DamageTypeToClass(type);
            if (SupportedDamageClasses.Contains(classType))
            {
               _damageList[type] = newValue;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Sets the damage value for the given <see cref="DamageType"/>. Does not check whether the value is supported, and so will throw an error if an invalid type is given.
        /// </summary>
        public void SetDamageValue(DamageType type, int newValue)
        {
            if (newValue < 0)
                return;
            _damageList[type] = newValue;
        }
    }
}
