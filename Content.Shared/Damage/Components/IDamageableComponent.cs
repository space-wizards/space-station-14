using System.Collections.Generic;
using Content.Shared.Acts;
using Content.Shared.Damage.Resistances;
using Robust.Shared.GameObjects;

namespace Content.Shared.Damage.Components
{
    public interface IDamageableComponent : IComponent, IExAct
    {
        /// <summary>
        ///     Sum of all damages taken.
        /// </summary>
        int TotalDamage { get; }

        /// <summary>
        ///     The amount of damage mapped by <see cref="DamageGroupPrototype"/>.
        /// </summary>
        IReadOnlyDictionary<DamageGroupPrototype, int> DamageGroups { get; }

        /// <summary>
        ///     The amount of damage mapped by <see cref="DamageTypePrototype"/>.
        /// </summary>
        IReadOnlyDictionary<DamageTypePrototype, int> DamageTypes { get; }

        /// <summary>
        ///     The amount of damage mapped by the string IDs of <see cref="DamageGroupPrototype"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int> DamageGroupIDs { get;  }

        /// <summary>
        ///     The amount of damage mapped by the string IDs of <see cref="DamageTypePrototype"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int> DamageTypeIDs { get; }

        HashSet<DamageTypePrototype> SupportedTypes { get; }

        HashSet<DamageGroupPrototype> SupportedGroups { get; }

        /// <summary>
        ///     The resistances of this component.
        /// </summary>
        ResistanceSet Resistances { get; }

        bool SupportsDamageGroup(DamageGroupPrototype group);

        bool SupportsDamageType(DamageTypePrototype type);

        /// <summary>
        ///     Gets the amount of damage of a type.
        /// </summary>
        /// <param name="type">The type to get the damage of.</param>
        /// <param name="damage">The amount of damage of that type.</param>
        /// <returns>
        ///     True if the given <see cref="type"/> is supported, false otherwise.
        /// </returns>
        bool TryGetDamage(DamageTypePrototype type, out int damage);

        /// <summary>
        ///     Gets the total amount of damage in a damage group.
        /// </summary>
        /// <param name="group">The group to get the damage of.</param>
        /// <param name="damage">The amount of damage in that group.</param>
        /// <returns>
        ///     True if the given <see cref="@group"/> is supported, false otherwise.
        /// </returns>
        bool TryGetDamage(DamageGroupPrototype group, out int damage);

        /// <summary>
        ///     Changes the specified <see cref="DamageTypePrototype"/>, applying
        ///     resistance values only if it is damage.
        /// </summary>
        /// <param name="type">Type of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreResistances">
        ///     Whether or not to ignore resistances.
        ///     Healing always ignores resistances, regardless of this input.
        /// </param>
        /// <param name="source">
        ///     The entity that dealt or healed the damage, if any.
        /// </param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     False if the given type is not supported or improper
        ///     <see cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool ChangeDamage(
            DamageTypePrototype type,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Changes the specified <see cref="DamageGroupPrototype"/>, applying
        ///     resistance values only if it is damage.
        ///     Spreads amount evenly between the <see cref="DamageTypePrototype"></see>s
        ///     represented by that group.
        ///     Note that if only a subset of the damage types in the group are actually supported, then the
        ///     total change change will be less than expected.
        /// </summary>
        /// <param name="group">group of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreResistances">
        ///     Whether to ignore resistances.
        ///     Healing always ignores resistances, regardless of this input.
        /// </param>
        /// <param name="source">Entity that dealt or healed the damage, if any.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require,
        ///     such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given group is not supported or improper
        ///     <see cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool ChangeDamage(
            DamageGroupPrototype group,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageTypePrototype"/> to the given
        ///     value, ignoring resistance values.
        /// </summary>
        /// <param name="type">Type of damage being changed.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require,
        ///     such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given type is not supported or improper
        ///     <see cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool SetDamage(
            DamageTypePrototype type,
            int newValue,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Sets all supported damage types to specified value using <see cref="SetDamage"></see>.
        /// </summary>
        void SetAllDamage(int newValue);

        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health.
        /// </summary>
        void ForceHealthChangedEvent();
    }
}
