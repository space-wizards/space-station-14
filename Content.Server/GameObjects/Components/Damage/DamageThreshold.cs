using System;
using Content.Shared.GameObjects;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Triggers an event when values rise above or drop below this threshold
    /// </summary>
    public struct DamageThreshold
    {
        public DamageType DamageType { get; }
        public int Value { get; }
        public ThresholdType ThresholdType { get; }

        public DamageThreshold(DamageType damageType, int value, ThresholdType thresholdType)
        {
            DamageType = damageType;
            Value = value;
            ThresholdType = thresholdType;
        }

        public override bool Equals(Object obj)
        {
            return obj is DamageThreshold threshold && this == threshold;
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

    public enum ThresholdType
    {
        None,
        Destruction,
        Death,
        Critical,
        HUDUpdate,
        Breakage,
    }

    public class DamageThresholdPassedEventArgs : EventArgs
    {
        public DamageThreshold DamageThreshold { get; }
        public bool Passed { get; }
        public int ExcessDamage { get; }

        public DamageThresholdPassedEventArgs(DamageThreshold threshold, bool passed, int excess)
        {
            DamageThreshold = threshold;
            Passed = passed;
            ExcessDamage = excess;
        }
    }

    public class DamageEventArgs : EventArgs
    {
        /// <summary>
        ///     Type of damage.
        /// </summary>
        public DamageType Type { get; }

        /// <summary>
        ///     Change in damage.
        /// </summary>
        public int Damage { get; }

        /// <summary>
        ///     The entity that damaged this one.
        ///     Could be null.
        /// </summary>
        public IEntity Source { get; }

        /// <summary>
        ///     The mob entity that damaged this one.
        ///     Could be null.
        /// </summary>
        public IEntity SourceMob { get; }

        public DamageEventArgs(DamageType type, int damage, IEntity source, IEntity sourceMob)
        {
            Type = type;
            Damage = damage;
            Source = source;
            SourceMob = sourceMob;
        }
    }
}

