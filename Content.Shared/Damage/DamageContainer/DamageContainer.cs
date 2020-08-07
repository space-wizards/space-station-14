using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.DamageContainer
{
    /// <summary>
    ///     Holds the information regarding the various forms of damage an object has
    ///     taken (i.e. brute, burn, or toxic damage).
    /// </summary>
    [Serializable, NetSerializable]
    public class DamageContainer
    {
        private Dictionary<DamageType, int> _damageList = DamageTypeExtensions.ToDictionary();

        public DamageContainer(DamageContainerPrototype data)
        {
            SupportedDamageClasses = data.ActiveDamageClasses;
        }

        public DamageContainer(List<DamageClass> supportedClasses)
        {
            SupportedDamageClasses = supportedClasses;
        }

        public DamageContainer() { }

        [ViewVariables] public virtual List<DamageClass> SupportedDamageClasses { get; }

        [ViewVariables]
        public virtual List<DamageType> SupportedDamageTypes
        {
            get
            {
                var toReturn = new List<DamageType>();
                foreach (var damageClass in SupportedDamageClasses)
                {
                    toReturn.AddRange(damageClass.ToType());
                }

                return toReturn;
            }
        }

        /// <summary>
        ///     Sum of all damages kept on record.
        /// </summary>
        [ViewVariables]
        public int TotalDamage => _damageList.Values.Sum();

        public bool SupportsDamageClass(DamageClass damageClass)
        {
            return SupportedDamageClasses.Contains(damageClass);
        }

        public bool SupportsDamageType(DamageType damageType)
        {
            return SupportedDamageClasses.Contains(damageType.ToClass());
        }

        /// <summary>
        ///     Attempts to grab the damage value for the given <see cref="DamageType" />. Returns false if the container does not
        ///     support that type.
        /// </summary>
        public bool TryGetDamageValue(DamageType type, out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }

        /// <summary>
        ///     Grabs the damage value for the given <see cref="DamageType" />.
        /// </summary>
        public int GetDamageValue(DamageType type)
        {
            return TryGetDamageValue(type, out var damage) ? damage : 0;
        }

        /// <summary>
        ///     Attempts to grab the sum of damage values for the given
        ///     <see cref="DamageClass"/>.
        /// </summary>
        /// <returns>
        ///     True if the class is supported in this container, false otherwise.
        /// </returns>
        public bool TryGetDamageClassSum(DamageClass damageClass, out int damage)
        {
            damage = 0;

            if (SupportsDamageClass(damageClass))
            {
                foreach (var type in damageClass.ToType())
                {
                    damage += GetDamageValue(type);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Grabs the sum of damage values for the given <see cref="DamageClass"/>.
        /// </summary>
        public int GetDamageClassSum(DamageClass damageClass)
        {
            var sum = 0;
            foreach (var type in damageClass.ToType())
            {
                sum += GetDamageValue(type);
            }

            return sum;
        }

        /// <summary>
        ///     Attempts to change the damage value for the given
        ///     <see cref="DamageType"/>
        /// </summary>
        /// <returns>
        ///     True if successful, false if this container does not support that type.
        /// </returns>
        public bool TryChangeDamageValue(DamageType type, int delta)
        {
            var damageClass = type.ToClass();

            if (SupportedDamageClasses.Contains(damageClass))
            {
                _damageList[type] = _damageList[type] + delta;
                if (_damageList[type] < 0)
                {
                    _damageList[type] = 0;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Changes the damage value for the given <see cref="DamageType"/>.
        /// </summary>
        /// <returns>
        ///     True if successful, false if this container does not support that type.
        /// </returns>
        public bool ChangeDamageValue(DamageType type, int delta)
        {
            if (!_damageList.TryGetValue(type, out var current))
            {
                return false;
            }

            _damageList[type] = current + delta;

            if (_damageList[type] < 0)
            {
                _damageList[type] = 0;
            }

            return true;
        }

        /// <summary>
        ///     Attempts to set the damage value for the given <see cref="DamageType" />. Returns false if the container does not
        ///     support that type, or if there was an error.
        /// </summary>
        /// <returns>
        ///     True if successful, false if this container does not support that type.
        /// </returns>
        public bool TrySetDamageValue(DamageType type, int newValue)
        {
            if (newValue < 0)
            {
                return false;
            }

            var damageClass = type.ToClass();

            if (SupportedDamageClasses.Contains(damageClass))
            {
                _damageList[type] = newValue;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to set the damage value for the given <see cref="DamageType" />.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetDamageValue(DamageType type, int newValue)
        {
            if (newValue < 0)
            {
                return false;
            }

            if (!_damageList.ContainsKey(type))
            {
                return false;
            }

            _damageList[type] = newValue;

            return true;
        }
    }
}
