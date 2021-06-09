#nullable enable
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Damage
{
    public interface IDamageableComponent : IComponent, IExAct
    {
        /// <summary>
        ///     Sum of all damages taken.
        /// </summary>
        int TotalDamage { get; }

        /// <summary>
        ///     The amount of damage mapped by <see cref="DamageClass"/>.
        /// </summary>
        IReadOnlyDictionary<DamageClass, int> DamageClasses { get; }

        /// <summary>
        ///     The amount of damage mapped by <see cref="DamageType"/>.
        /// </summary>
        IReadOnlyDictionary<DamageType, int> DamageTypes { get; }

        /// <summary>
        ///     The damage flags on this component.
        /// </summary>
        DamageFlag Flags { get; }

        /// <summary>
        ///     Adds a flag to this component.
        /// </summary>
        /// <param name="flag">The flag to add.</param>
        void AddFlag(DamageFlag flag);

        /// <summary>
        ///     Checks whether or not this component has a specific flag.
        /// </summary>
        /// <param name="flag">The flag to check for.</param>
        /// <returns>True if it has the flag, false otherwise.</returns>
        bool HasFlag(DamageFlag flag);

        /// <summary>
        ///     Removes a flag from this component.
        /// </summary>
        /// <param name="flag">The flag to remove.</param>
        void RemoveFlag(DamageFlag flag);

        bool SupportsDamageClass(DamageClass @class);

        bool SupportsDamageType(DamageType type);

        /// <summary>
        ///     Gets the amount of damage of a type.
        /// </summary>
        /// <param name="type">The type to get the damage of.</param>
        /// <param name="damage">The amount of damage of that type.</param>
        /// <returns>
        ///     True if the given <see cref="type"/> is supported, false otherwise.
        /// </returns>
        bool TryGetDamage(DamageType type, out int damage);

        /// <summary>
        ///     Gets the amount of damage of a class.
        /// </summary>
        /// <param name="class">The class to get the damage of.</param>
        /// <param name="damage">The amount of damage of that class.</param>
        /// <returns>
        ///     True if the given <see cref="@class"/> is supported, false otherwise.
        /// </returns>
        bool TryGetDamage(DamageClass @class, out int damage);

        /// <summary>
        ///     Changes the specified <see cref="DamageType"/>, applying
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
            DamageType type,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Changes the specified <see cref="DamageClass"/>, applying
        ///     resistance values only if it is damage.
        ///     Spreads amount evenly between the <see cref="DamageType"></see>s
        ///     represented by that class.
        /// </summary>
        /// <param name="class">Class of damage being changed.</param>
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
        ///     Returns false if the given class is not supported or improper
        ///     <see cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool ChangeDamage(
            DamageClass @class,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageType"/> to the given
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
            DamageType type,
            int newValue,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Sets all damage values to zero.
        /// </summary>
        void Heal();

        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health.
        /// </summary>
        void ForceHealthChangedEvent();
    }
}
