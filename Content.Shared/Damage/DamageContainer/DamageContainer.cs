using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Damage;
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

        public delegate void HealthChangedDelegate(List<HealthChangeData> changes);

        [NonSerialized] public readonly HealthChangedDelegate OnHealthChanged;

        public DamageContainer(HealthChangedDelegate onHealthChanged, DamageContainerPrototype data)
        {
            ID = data.ID;
            OnHealthChanged = onHealthChanged;
            SupportedClasses = data.ActiveDamageClasses;
        }

        public string ID { get; }

        [ViewVariables] public virtual List<DamageClass> SupportedClasses { get; }

        [ViewVariables]
        public virtual List<DamageType> SupportedTypes
        {
            get
            {
                var toReturn = new List<DamageType>();
                foreach (var @class in SupportedClasses)
                {
                    toReturn.AddRange(@class.ToTypes());
                }

                return toReturn;
            }
        }

        /// <summary>
        ///     Sum of all damages kept on record.
        /// </summary>
        [ViewVariables]
        public int TotalDamage => _damageList.Values.Sum();

        public IReadOnlyDictionary<DamageClass, int> DamageClasses =>
            DamageTypeExtensions.ToClassDictionary(DamageTypes);

        public IReadOnlyDictionary<DamageType, int> DamageTypes => _damageList;

        public bool SupportsDamageClass(DamageClass @class)
        {
            return SupportedClasses.Contains(@class);
        }

        public bool SupportsDamageType(DamageType type)
        {
            return SupportedClasses.Contains(type.ToClass());
        }

        /// <summary>
        ///     Attempts to grab the damage value for the given <see cref="DamageType"/>.
        /// </summary>
        /// <returns>
        ///     False if the container does not support that type, true otherwise.
        /// </returns>
        public bool TryGetDamageValue(DamageType type, [NotNullWhen(true)] out int damage)
        {
            return _damageList.TryGetValue(type, out damage);
        }

        /// <summary>
        ///     Grabs the damage value for the given <see cref="DamageType"/>.
        /// </summary>
        public int GetDamageValue(DamageType type)
        {
            return TryGetDamageValue(type, out var damage) ? damage : 0;
        }

        /// <summary>
        ///     Attempts to grab the sum of damage values for the given
        ///     <see cref="DamageClasses"/>.
        /// </summary>
        /// <param name="class">The class to get the sum for.</param>
        /// <param name="damage">The resulting amount of damage, if any.</param>
        /// <returns>
        ///     True if the class is supported in this container, false otherwise.
        /// </returns>
        public bool TryGetDamageClassSum(DamageClass @class, [NotNullWhen(true)] out int damage)
        {
            damage = 0;

            if (SupportsDamageClass(@class))
            {
                foreach (var type in @class.ToTypes())
                {
                    damage += GetDamageValue(type);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Grabs the sum of damage values for the given <see cref="DamageClasses"/>.
        /// </summary>
        public int GetDamageClassSum(DamageClass damageClass)
        {
            var sum = 0;

            foreach (var type in damageClass.ToTypes())
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

            if (SupportsDamageClass(damageClass))
            {
                var current = _damageList[type];
                current = _damageList[type] = current + delta;

                if (_damageList[type] < 0)
                {
                    _damageList[type] = 0;
                    delta = -current;
                    current = 0;
                }

                var datum = new HealthChangeData(type, current, delta);
                var data = new List<HealthChangeData> {datum};

                OnHealthChanged(data);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Changes the damage value for the given <see cref="DamageType"/>.
        /// </summary>
        /// <param name="type">The type of damage to change.</param>
        /// <param name="delta">The amount to change it by.</param>
        /// <param name="quiet">
        ///     Whether or not to suppress the health change event.
        /// </param>
        /// <returns>
        ///     True if successful, false if this container does not support that type.
        /// </returns>
        public bool ChangeDamageValue(DamageType type, int delta, bool quiet = false)
        {
            if (!_damageList.TryGetValue(type, out var current))
            {
                return false;
            }

            _damageList[type] = current + delta;

            if (_damageList[type] < 0)
            {
                _damageList[type] = 0;
                delta = -current;
            }

            current = _damageList[type];

            var datum = new HealthChangeData(type, current, delta);
            var data = new List<HealthChangeData> {datum};

            OnHealthChanged(data);

            return true;
        }

        /// <summary>
        ///     Attempts to set the damage value for the given <see cref="DamageType"/>.
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

            if (SupportedClasses.Contains(damageClass))
            {
                var old = _damageList[type] = newValue;
                _damageList[type] = newValue;

                var delta = newValue - old;
                var datum = new HealthChangeData(type, newValue, delta);
                var data = new List<HealthChangeData> {datum};

                OnHealthChanged(data);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to set the damage value for the given <see cref="DamageType"/>.
        /// </summary>
        /// <param name="type">The type of damage to set.</param>
        /// <param name="newValue">The value to set it to.</param>
        /// <param name="quiet">
        ///     Whether or not to suppress the health changed event.
        /// </param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool SetDamageValue(DamageType type, int newValue, bool quiet = false)
        {
            if (newValue < 0)
            {
                return false;
            }

            if (!_damageList.ContainsKey(type))
            {
                return false;
            }

            var old = _damageList[type];
            _damageList[type] = newValue;

            if (!quiet)
            {
                var delta = newValue - old;
                var datum = new HealthChangeData(type, 0, delta);
                var data = new List<HealthChangeData> {datum};

                OnHealthChanged(data);
            }

            return true;
        }

        public void Heal()
        {
            var data = new List<HealthChangeData>();

            foreach (var type in SupportedTypes)
            {
                var delta = -GetDamageValue(type);
                var datum = new HealthChangeData(type, 0, delta);

                data.Add(datum);
                SetDamageValue(type, 0, true);
            }

            OnHealthChanged(data);
        }
    }
}
