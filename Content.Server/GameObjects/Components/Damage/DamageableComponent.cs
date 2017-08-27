using System;
using System.Collections.Generic;
using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Server.Interfaces;

namespace Content.Server.GameObjects
{
    //TODO: add support for component add/remove

    /// <summary>
    /// A component that handles receiving damage and healing,
    /// as well as informing other components of it.
    /// </summary>
    public class DamageableComponent : Component
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

        Dictionary<DamageType, int> CurrentDamage = new Dictionary<DamageType, int>();
        Dictionary<DamageType, List<int>> Thresholds = new Dictionary<DamageType, List<int>>();

        public event EventHandler<DamageThresholdPassedEventArgs> DamageThresholdPassed;
        protected virtual void OnDamageThresholdPassed(DamageThresholdPassedEventArgs e)
        {
            DamageThresholdPassed?.Invoke(this, e);
        }

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;

            if (mapping.TryGetNode("resistanceset", out node))
                Resistances = ResistanceSet.GetResistanceSet(node.AsString());
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            InitializeDamageType(DamageType.Total);

            AddThresholdsFrom(Owner as IOnDamageBehaviour);

            RecalculateComponentThresholds();
        }

        /// <summary>
        /// The function that handles receiving damage.
        /// Converts damage via the resistance set then applies it
        /// and informs components of thresholds passed as necessary.
        /// </summary>
        /// <param name="damageType">Type of damage being received.</param>
        /// <param name="amount">Amount of damage being received.</param>
        public void TakeDamage(DamageType damageType, int amount)
        {
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

        /// <summary>
        /// Handles receiving healing.
        /// Converts healing via the resistance set then applies it
        /// and informs components of thresholds passed as necessary.
        /// </summary>
        /// <param name="damageType">Type of damage being received.</param>
        /// <param name="amount">Amount of damage being received.</param>
        public void TakeHealing(DamageType damageType, int amount)
        {
            TakeDamage(damageType, -amount);
        }

        void UpdateForDamageType(DamageType damageType, int oldValue)
        {
            int change = CurrentDamage[damageType] - oldValue;

            if (change == 0)
                return;

            int changeSign = Math.Sign(change);

            foreach (int value in Thresholds[damageType])
            {
                if (((value * changeSign) > (oldValue * changeSign)) && ((value * changeSign) <= (CurrentDamage[damageType] * changeSign)))
                {
                    OnDamageThresholdPassed(new DamageThresholdPassedEventArgs(new DamageThreshold(damageType, value), (changeSign > 0)));
                }
            }
        }

        void RecalculateComponentThresholds()
        {
            foreach (IOnDamageBehaviour onDamageBehaviourComponent in Owner.GetComponents<IOnDamageBehaviour>())
            {
                AddThresholdsFrom(onDamageBehaviourComponent);
            }
        }

        void AddThresholdsFrom(IOnDamageBehaviour onDamageBehaviour)
        {
            if (onDamageBehaviour == null)
                return;

            List<DamageThreshold> thresholds = onDamageBehaviour.GetAllDamageThresholds();

            foreach (DamageThreshold threshold in thresholds)
            {
                if (!Thresholds[threshold.DamageType].Contains(threshold.Value))
                {
                    Thresholds[threshold.DamageType].Add(threshold.Value);
                }
            }
        }

        void InitializeDamageType(DamageType damageType)
        {
            if (!CurrentDamage.ContainsKey(damageType))
            {
                CurrentDamage.Add(damageType, 0);
                Thresholds.Add(damageType, new List<int>());
            }
        }
    }

    public struct DamageThreshold
    {
        public DamageType DamageType { get; private set; }
        public int Value { get; private set; }

        public DamageThreshold(DamageType damageType, int value)
        {
            DamageType = damageType;
            Value = value;
        }

        public override bool Equals(Object obj)
        {
            return obj is DamageThreshold && this == (DamageThreshold)obj;
        }
        public override int GetHashCode()
        {
            return DamageType.GetHashCode() ^ Value.GetHashCode();
        }
        public static bool operator ==(DamageThreshold x, DamageThreshold y)
        {
            return x.DamageType == y.DamageType && x.Value == y.Value;
        }
        public static bool operator !=(DamageThreshold x, DamageThreshold y)
        {
            return !(x == y);
        }
    }

    public class DamageThresholdPassedEventArgs : EventArgs
    {
        public DamageThreshold DamageThreshold { get; set; }
        public bool Passed { get; set; }

        public DamageThresholdPassedEventArgs(DamageThreshold threshold, bool passed)
        {
            DamageThreshold = threshold;
            Passed = passed;
        }
    }
}

