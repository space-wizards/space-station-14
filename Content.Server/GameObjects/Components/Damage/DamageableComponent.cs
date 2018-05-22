using Content.Server.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using SS14.Shared.Maths;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects
{
    //TODO: add support for component add/remove

    /// <summary>
    /// A component that handles receiving damage and healing,
    /// as well as informing other components of it.
    /// </summary>
    public class DamageableComponent : Component, IDamageableComponent
    {
        /// <inheritdoc />
        public override string Name => "Damageable";

        /// <inheritdoc />
        public override uint? NetID => ContentNetIDs.DAMAGEABLE;

        /// <summary>
        /// The resistance set of this object.
        /// Affects receiving damage of various types.
        /// </summary>
        public ResistanceSet Resistances { get; private set; }

        public Dictionary<DamageType, int> CurrentDamage = new Dictionary<DamageType, int>();
        Dictionary<DamageType, List<DamageThreshold>> Thresholds = new Dictionary<DamageType, List<DamageThreshold>>();

        public event EventHandler<DamageThresholdPassedEventArgs> DamageThresholdPassed;


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
            }

            RecalculateComponentThresholds();
        }

        /// <inheritdoc />
        public void TakeDamage(DamageType damageType, int amount)
        {
            if (damageType == DamageType.Total)
            {
                throw new ArgumentException("Cannot take damage for DamageType.Total");
            }
            InitializeDamageType(damageType);

            int oldValue = CurrentDamage[damageType];
            int oldTotalValue = -1;

            if (amount == 0)
            {
                return;
            }

            amount = Resistances.CalculateDamage(damageType, amount);
            CurrentDamage[damageType] = Math.Max(0, CurrentDamage[damageType] + amount);
            UpdateForDamageType(damageType, oldValue);

            if (Resistances.AppliesToTotal(damageType))
            {
                oldTotalValue = CurrentDamage[DamageType.Total];
                CurrentDamage[DamageType.Total] = Math.Max(0, CurrentDamage[DamageType.Total] + amount);
                UpdateForDamageType(DamageType.Total, oldTotalValue);
            }
        }

        /// <inheritdoc />
        public void TakeHealing(DamageType damageType, int amount)
        {
            if (damageType == DamageType.Total)
            {
                throw new ArgumentException("Cannot heal for DamageType.Total");
            }
            TakeDamage(damageType, -amount);
        }

        void UpdateForDamageType(DamageType damageType, int oldValue)
        {
            int change = CurrentDamage[damageType] - oldValue;

            if (change == 0)
            {
                return;
            }

            int changeSign = Math.Sign(change);

            foreach (var threshold in Thresholds[damageType])
            {
                var value = threshold.Value;
                if (((value * changeSign) > (oldValue * changeSign)) && ((value * changeSign) <= (CurrentDamage[damageType] * changeSign)))
                {
                    var args = new DamageThresholdPassedEventArgs(threshold, (changeSign > 0));
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
            if (!CurrentDamage.ContainsKey(damageType))
            {
                CurrentDamage.Add(damageType, 0);
                Thresholds.Add(damageType, new List<DamageThreshold>());
            }
        }
    }
}

