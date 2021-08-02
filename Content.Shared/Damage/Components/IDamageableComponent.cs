using System.Collections.Generic;
using Content.Shared.Acts;
using Content.Shared.Damage.Resistances;
using Robust.Shared.GameObjects;

namespace Content.Shared.Damage.Components
{
    public interface IDamageableComponent : IComponent, IExAct
    {
        /// <summary>
        ///     The sum of all damages types in the DamageableComponent.
        /// </summary>
        int TotalDamage { get; }

        /// <summary>
        /// Returns a dictionary of the damage in the container, indexed by <see cref="DamageGroupPrototype"/>.
        /// </summary>
        /// <remarks>
        /// The values represent the sum of all damage in each group. If a supported damage type is a member of more than one group, it will contribute to each one.
        /// Therefore, the sum of the values may be greater than the sum of the values in the dictionary returned by <see cref="DamagePerType"/>
        /// </remarks>
        IReadOnlyDictionary<DamageGroupPrototype, int> DamagePerGroup { get; }

        /// <summary>
        /// Returns a dictionary of the damage in the container, indexed by supported instances of <see
        /// cref="DamageGroupPrototype"/>.
        /// </summary>
        /// <remarks>
        /// The values represent the sum of all damage in each group. As the damage container may have some damage
        /// types that are not part of a fully supported damage group, the sum of the values may be less of the values
        /// in the dictionary returned by <see cref="DamagePerType"/>. On the other hand, if a supported damage type
        /// is a member of more than one group, it will contribute to each one. Therefore, the sum may also be greater
        /// instead.
        /// </remarks>
        IReadOnlyDictionary<DamageGroupPrototype, int> DamagePerSupportedGroup { get; }

        /// <summary>
        /// Returns a dictionary of the damage in the container, indexed by <see cref="DamageTypePrototype"/>.
        /// </summary>
        IReadOnlyDictionary<DamageTypePrototype, int> DamagePerType { get; }

        /// <summary>
        /// Like <see cref="DamagePerGroup"/>, but indexed by <see cref="DamageGroupPrototype.ID"/>
        /// </summary>
        public IReadOnlyDictionary<string, int> DamagePerGroupIDs { get;  }

        /// <summary>
        /// Like <see cref="DamagePerSupportedGroup"/>, but indexed by <see cref="DamageGroupPrototype.ID"/>
        /// </summary>
        public IReadOnlyDictionary<string, int> DamagePerSupportedGroupIDs { get; }

        /// <summary>
        /// Like <see cref="DamagePerType"/>, but indexed by <see cref="DamageGroupType.ID"/>
        /// </summary>
        public IReadOnlyDictionary<string, int> DamagePerTypeIDs { get; }

        /// <summary>
        /// Collection of damage types supported by this DamageableComponent.
        /// </summary>
        /// <remarks>
        /// Each of these damage types is fully supported. If any of these damage types is a
        /// member of a damage group, these groups are represented in <see cref="ApplicableDamageGroups"></see>
        /// </remarks>
        HashSet<DamageTypePrototype> SupportedDamageTypes { get; }

        /// <summary>
        /// Collection of damage groups that are fully supported by DamageableComponent.
        /// </summary>
        /// <remarks>
        /// This describes what damage groups this damage container explicitly supports. It supports every damage type
        /// contained in these damage groups. It may also support other damage types not in these groups. To see all
        /// damage types <see cref="SupportedDamageTypes"/>, and to see all applicable damage groups <see
        /// cref="ApplicableDamageGroups"/>.
        /// </remarks>
        public HashSet<DamageGroupPrototype> SupportedDamageGroups { get;  }

        /// <summary>
        /// Collection of damage groups that could affect this DamageableComponent.
        /// </summary>
        /// <remarks>
        /// This describes what damage groups could have an effect on this damage container. However not every damage
        /// group has to be fully supported. For example, the container may support ONLY the piercing damage type. It should
        /// therefore be affected by instances of brute damage, but does not necessarily support blunt or slash damage.
        /// For a list of supported damage types, see <see cref="SupportedDamageTypes"/>.
        /// </remarks>
        HashSet<DamageGroupPrototype> ApplicableDamageGroups { get; }

        /// <summary>
        ///     The resistances of this component.
        /// </summary>
        ResistanceSet Resistances { get; }

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
        ///     True if the given <see cref="@group"/> is applicable to this container, false otherwise.
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
        /// <param name="ignoreDamageResistances">
        ///     Whether or not to ignore resistances when taking damage.
        ///     Healing always ignores resistances, regardless of this input.
        /// </param>
        /// <param name="source">
        ///     The entity that dealt or healed the damage, if any.
        /// </param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     False if the given type is not supported, no damage change occurred, or improper
        ///     <see cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool TryChangeDamage(
            DamageTypePrototype type,
            int amount,
            bool ignoreDamageResistances = false,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Changes damage of the specified <see cref="DamageGroupPrototype"/>, applying resistance values only if
        ///     it is damage.
        /// </summary>
        /// <remarks>
        ///     This spreads the damage change amount evenly between the <see cref="DamageTypePrototype"></see>s in this
        ///     group (subject to integer rounding). Note that if only a subset of the damage types in the group are
        ///     actually supported by the container, then the total change will be less than expected.
        /// </remarks>
        /// <param name="group">group of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreDamageResistances">
        ///     Whether to ignore resistances when taking damage. Healing always ignores resistances, regardless of this
        ///     input.
        /// </param>
        /// <param name="source">Entity that dealt or healed the damage, if any.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given group is not applicable,  no damage change occurred, or improper <see
        ///     cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool TryChangeDamage(
            DamageGroupPrototype group,
            int amount,
            bool ignoreDamageResistances = false,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageTypePrototype"/> to the given value, ignoring resistance
        ///     values.
        /// </summary>
        /// <param name="type">Type of damage being set.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given type is not supported, no damage change occurred, or improper <see
        ///     cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool TrySetDamage(
            DamageTypePrototype type,
            int newValue,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Forcefully sets all damage types in a specified damage group using <see cref="TrySetDamage"></see>.
        /// </summary>
        /// <param name="group">Group of damage being set.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if the given group is not applicable,  no damage change occurred, or improper <see
        ///     cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool TrySetDamage(
            DamageGroupPrototype group,
            int newValue,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Sets all supported damage types to specified value using <see cref="TrySetDamage"></see>.
        /// </summary>
        /// <param name="group">Group of damage being set.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <param name="source">Entity that set the new damage value.</param>
        /// <param name="extraParams">
        ///     Extra parameters that some components may require, such as a specific limb to target.
        /// </param>
        /// <returns>
        ///     Returns false if no damage change occurred or improper <see
        ///     cref="DamageChangeParams"/> were provided; true otherwise.
        /// </returns>
        bool TrySetAllDamage(
            int newValue,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health.
        /// </summary>
        void ForceHealthChangedEvent();
    }
}
