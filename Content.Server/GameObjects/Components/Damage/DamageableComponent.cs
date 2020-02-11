using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    //TODO: add support for component add/remove

    /// <summary>
    /// A component that handles receiving damage and healing,
    /// as well as informing other components of it.
    /// </summary>
    [RegisterComponent]
    public class DamageableComponent : SharedDamageableComponent, IDamageableComponent
    {
        /// <inheritdoc />
        public override string Name => "Damageable";

        /// <summary>
        /// The resistance set of this object.
        /// Affects receiving damage of various types.
        /// </summary>
        [ViewVariables]
        public ResistanceSet Resistances { get; private set; }

        [ViewVariables]
        public IReadOnlyDictionary<DamageType, int> CurrentDamage => _currentDamage;
        private Dictionary<DamageType, int> _currentDamage = new Dictionary<DamageType, int>();

        Dictionary<DamageType, List<DamageThreshold>> Thresholds = new Dictionary<DamageType, List<DamageThreshold>>();

        public event EventHandler<DamageThresholdPassedEventArgs> DamageThresholdPassed;
        public event EventHandler<DamageEventArgs> Damaged;

        public override ComponentState GetComponentState()
        {
            return new DamageComponentState(_currentDamage);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
            serializer.DataReadFunction("resistanceset", "honk", name =>
            {
                Resistances = ResistanceSet.GetResistanceSet(name);
            });
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            InitializeDamageType(DamageType.Total);

            foreach (var damagebehavior in Owner.GetAllComponents<IOnDamageBehavior>())
            {
                AddThresholdsFrom(damagebehavior);
                Damaged += damagebehavior.OnDamaged;
            }

            RecalculateComponentThresholds();
        }

        /// <inheritdoc />
        public void TakeDamage(DamageType damageType, int amount, IEntity source = null, IEntity sourceMob = null)
        {
            if (damageType == DamageType.Total)
            {
                throw new ArgumentException("Cannot take damage for DamageType.Total");
            }
            InitializeDamageType(damageType);

            int oldValue = _currentDamage[damageType];
            int oldTotalValue = -1;

            if (amount == 0)
            {
                return;
            }

            amount = Resistances.CalculateDamage(damageType, amount);
            _currentDamage[damageType] = Math.Max(0, _currentDamage[damageType] + amount);
            UpdateForDamageType(damageType, oldValue);

            Damaged?.Invoke(this, new DamageEventArgs(damageType, amount, source, sourceMob));

            if (Resistances.AppliesToTotal(damageType))
            {
                oldTotalValue = _currentDamage[DamageType.Total];
                _currentDamage[DamageType.Total] = Math.Max(0, _currentDamage[DamageType.Total] + amount);
                UpdateForDamageType(DamageType.Total, oldTotalValue);
            }
        }

        /// <inheritdoc />
        public void TakeHealing(DamageType damageType, int amount, IEntity source = null, IEntity sourceMob = null)
        {
            if (damageType == DamageType.Total)
            {
                throw new ArgumentException("Cannot heal for DamageType.Total");
            }
            TakeDamage(damageType, -amount, source, sourceMob);
        }

        public void HealAllDamage()
        {
            var values = Enum.GetValues(typeof(DamageType)).Cast<DamageType>();
            foreach (var damageType in values)
            {
                if (CurrentDamage.ContainsKey(damageType) && damageType != DamageType.Total)
                {
                    TakeHealing(damageType, CurrentDamage[damageType]);
                }
            }
        }

        void UpdateForDamageType(DamageType damageType, int oldValue)
        {
            int change = _currentDamage[damageType] - oldValue;

            if (change == 0)
            {
                return;
            }

            int changeSign = Math.Sign(change);

            foreach (var threshold in Thresholds[damageType])
            {
                var value = threshold.Value;
                if (((value * changeSign) > (oldValue * changeSign)) && ((value * changeSign) <= (_currentDamage[damageType] * changeSign)))
                {
                    var excessDamage = change - value;
                    var typeOfDamage = damageType;
                    if (change - value < 0)
                    {
                        excessDamage = 0;
                    }
                    var args = new DamageThresholdPassedEventArgs(threshold, (changeSign > 0), excessDamage);
                    DamageThresholdPassed?.Invoke(this, args);
                }
            }
        }

        void RecalculateComponentThresholds()
        {
            foreach (IOnDamageBehavior onDamageBehaviorComponent in Owner.GetAllComponents<IOnDamageBehavior>())
            {
                AddThresholdsFrom(onDamageBehaviorComponent);
            }
        }

        void AddThresholdsFrom(IOnDamageBehavior onDamageBehavior)
        {
            if (onDamageBehavior == null)
            {
                throw new ArgumentNullException(nameof(onDamageBehavior));
            }

            List<DamageThreshold> thresholds = onDamageBehavior.GetAllDamageThresholds();

            if (thresholds == null)
                return;

            foreach (DamageThreshold threshold in thresholds)
            {
                if (!Thresholds[threshold.DamageType].Contains(threshold))
                {
                    Thresholds[threshold.DamageType].Add(threshold);
                }
            }

            DamageThresholdPassed += onDamageBehavior.OnDamageThresholdPassed;
        }

        void InitializeDamageType(DamageType damageType)
        {
            if (!_currentDamage.ContainsKey(damageType))
            {
                _currentDamage.Add(damageType, 0);
                Thresholds.Add(damageType, new List<DamageThreshold>());
            }
        }
    }
}

