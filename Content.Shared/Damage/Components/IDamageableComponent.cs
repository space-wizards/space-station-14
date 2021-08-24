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
        ///     Returns a dictionary of the damage in the container, indexed by applicable <see cref="DamageGroupPrototype"/>.
        /// </summary>
        /// <remarks>
        ///     The values represent the sum of all damage in each group. If a supported damage type is a member of more than one group, it will contribute to each one.
        ///     Therefore, the sum of the values may be greater than the sum of the values in the dictionary returned by <see cref="GetDamagePerType"/>
        /// </remarks>
        IReadOnlyDictionary<DamageGroupPrototype, int> GetDamagePerApplicableGroup { get; }

        /// <summary>
        ///     Returns a dictionary of the damage in the container, indexed by fully supported instances of <see
        /// cref="DamageGroupPrototype"/>.
        /// </summary>
        /// <remarks>
        ///     The values represent the sum of all damage in each group. As the damage container may have some damage
        ///     types that are not part of a fully supported damage group, the sum of the values may be less of the values
        ///     in the dictionary returned by <see cref="GetDamagePerType"/>. On the other hand, if a supported damage type
        ///     is a member of more than one group, it will contribute to each one. Therefore, the sum may also be greater
        ///     instead.
        /// </remarks>
        IReadOnlyDictionary<DamageGroupPrototype, int> GetDamagePerFullySupportedGroup { get; }

        /// <summary>
        ///     Returns a dictionary of the damage in the container, indexed by <see cref="DamageTypePrototype"/>.
        /// </summary>
        IReadOnlyDictionary<DamageTypePrototype, int> GetDamagePerType { get; }

        /// <summary>
        ///     Like <see cref="GetDamagePerApplicableGroup"/>, but indexed by <see cref="DamageGroupPrototype.ID"/>
        /// </summary>
        IReadOnlyDictionary<string, int> GetDamagePerApplicableGroupIDs { get;  }

        /// <summary>
        ///     Like <see cref="GetDamagePerFullySupportedGroup"/>, but indexed by <see cref="DamageGroupPrototype.ID"/>
        /// </summary>
        IReadOnlyDictionary<string, int> GetDamagePerFullySupportedGroupIDs { get; }
<<<<<<< HEAD

<<<<<<< refs/remotes/origin/master
        bool SupportsDamageClass(DamageClass @class);

        bool SupportsDamageType(DamageType type);
=======
        /// <summary>
        ///     Like <see cref="GetDamagePerType"/>, but indexed by <see cref="DamageTypePrototype.ID"/>
        /// </summary>
        IReadOnlyDictionary<string, int> GetDamagePerTypeIDs { get; }

        /// <summary>
        ///     Collection of damage types supported by this DamageableComponent.
        /// </summary>
        /// <remarks>
        ///     Each of these damage types is fully supported. If any of these damage types is a
        ///     member of a damage group, these groups are represented in <see cref="ApplicableDamageGroups"></see>
        /// </remarks>
        HashSet<DamageTypePrototype> SupportedDamageTypes { get; }

        /// <summary>
        ///     Collection of damage groups that are fully supported by DamageableComponent.
        /// </summary>
        /// <remarks>
        ///     This describes what damage groups this damage container explicitly supports. It supports every damage type
        ///     contained in these damage groups. It may also support other damage types not in these groups. To see all
        ///     damage types <see cref="SupportedDamageTypes"/>, and to see all applicable damage groups <see
        /// cref="ApplicableDamageGroups"/>.
        /// </remarks>
        HashSet<DamageGroupPrototype> FullySupportedDamageGroups { get;  }
>>>>>>> Refactor damageablecomponent update (#4406)

        /// <summary>
        ///     Collection of damage groups that could apply damage to this DamageableComponent.
        /// </summary>
        /// <remarks>
        ///     This describes what damage groups could have an effect on this damage container. However not every damage
        ///     group has to be fully supported. For example, the container may support ONLY the piercing damage type. It should
        ///     therefore be affected by instances of brute damage, but does not necessarily support blunt or slash damage.
        ///     For a list of supported damage types, see <see cref="SupportedDamageTypes"/>.
        /// </remarks>
        HashSet<DamageGroupPrototype> ApplicableDamageGroups { get; }

=======

        /// <summary>
        ///     Like <see cref="GetDamagePerType"/>, but indexed by <see cref="DamageTypePrototype.ID"/>
        /// </summary>
        IReadOnlyDictionary<string, int> GetDamagePerTypeIDs { get; }

        /// <summary>
        ///     Collection of damage types supported by this DamageableComponent.
        /// </summary>
        /// <remarks>
        ///     Each of these damage types is fully supported. If any of these damage types is a
        ///     member of a damage group, these groups are represented in <see cref="ApplicableDamageGroups"></see>
        /// </remarks>
        HashSet<DamageTypePrototype> SupportedDamageTypes { get; }

        /// <summary>
        ///     Collection of damage groups that are fully supported by DamageableComponent.
        /// </summary>
        /// <remarks>
        ///     This describes what damage groups this damage container explicitly supports. It supports every damage type
        ///     contained in these damage groups. It may also support other damage types not in these groups. To see all
        ///     damage types <see cref="SupportedDamageTypes"/>, and to see all applicable damage groups <see
        /// cref="ApplicableDamageGroups"/>.
        /// </remarks>
        HashSet<DamageGroupPrototype> FullySupportedDamageGroups { get;  }

        /// <summary>
        ///     Collection of damage groups that could apply damage to this DamageableComponent.
        /// </summary>
        /// <remarks>
        ///     This describes what damage groups could have an effect on this damage container. However not every damage
        ///     group has to be fully supported. For example, the container may support ONLY the piercing damage type. It should
        ///     therefore be affected by instances of brute damage, but does not necessarily support blunt or slash damage.
        ///     For a list of supported damage types, see <see cref="SupportedDamageTypes"/>.
        /// </remarks>
        HashSet<DamageGroupPrototype> ApplicableDamageGroups { get; }

>>>>>>> refactor-damageablecomponent
        /// <summary>
        ///     The resistances of this component.
        /// </summary>
        ResistanceSet Resistances { get; }

        /// <summary>
        ///     Tries to get the amount of damage of a type.
        /// </summary>
        /// <param name="type">The type to get the damage of.</param>
        /// <param name="damage">The amount of damage of that type.</param>
        /// <returns>
        ///     True if the given <see cref="type"/> is supported, false otherwise.
        /// </returns>
        bool TryGetDamage(DamageTypePrototype type, out int damage);

        /// <summary>
        ///     Returns the amount of damage of a given type, or zero if it is not supported.
        /// </summary>
        int GetDamage(DamageTypePrototype type);

        /// <summary>
<<<<<<< HEAD
        ///     Returns the amount of damage of a given type, or zero if it is not supported.
        /// </summary>
        int GetDamage(DamageTypePrototype type);

        /// <summary>
        ///     Tries to get the total amount of damage in a damage group.
        /// </summary>
=======
        ///     Tries to get the total amount of damage in a damage group.
        /// </summary>
>>>>>>> refactor-damageablecomponent
        /// <param name="group">The group to get the damage of.</param>
        /// <param name="damage">The amount of damage in that group.</param>
        /// <returns>
        ///     True if the given group is applicable to this container, false otherwise.
        /// </returns>
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        bool TryGetDamage(DamageClass @class, out int damage);

        /// <summary>
        ///     Changes the specified <see cref="DamageType"/>, applying
        ///     resistance values only if it is damage.
=======
        bool TryGetDamage(DamageGroupPrototype group, out int damage);

        /// <summary>
        ///     Returns the amount of damage present in an applicable group, or zero if no members are supported.
        /// </summary>
        int GetDamage(DamageGroupPrototype group);

        /// <summary>
        ///     Tries to change the specified <see cref="DamageTypePrototype"/>, applying
        ///     resistance values only if it is dealing damage.
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        bool TryGetDamage(DamageGroupPrototype group, out int damage);

        /// <summary>
        ///     Returns the amount of damage present in an applicable group, or zero if no members are supported.
        /// </summary>
        int GetDamage(DamageGroupPrototype group);

        /// <summary>
        ///     Tries to change the specified <see cref="DamageTypePrototype"/>, applying
        ///     resistance values only if it is dealing damage.
>>>>>>> refactor-damageablecomponent
        /// </summary>
        /// <param name="type">Type of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreDamageResistances">
        ///     Whether or not to ignore resistances when taking damage.
        ///     Healing always ignores resistances, regardless of this input.
        /// </param>
        /// <returns>
        ///     False if the given type is not supported or no damage change occurred; true otherwise.
        /// </returns>
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
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
=======
        bool TryChangeDamage(DamageTypePrototype type, int amount, bool ignoreDamageResistances = false);

        /// <summary>
        ///     Tries to change damage of the specified <see cref="DamageGroupPrototype"/>, applying resistance values
        ///     only if it is damage.
>>>>>>> Refactor damageablecomponent update (#4406)
=======
        bool TryChangeDamage(DamageTypePrototype type, int amount, bool ignoreDamageResistances = false);

        /// <summary>
        ///     Tries to change damage of the specified <see cref="DamageGroupPrototype"/>, applying resistance values
        ///     only if it is damage.
>>>>>>> refactor-damageablecomponent
        /// </summary>
        /// <remarks>
        /// <para>
        ///     If dealing damage, this spreads the damage change amount evenly between the <see
        ///     cref="DamageTypePrototype"></see>s in this group (subject to integer rounding). If only a subset of the
        ///     damage types in the group are actually supported, then the total damage dealt may be less than expected
        ///     (unsupported damage is ignored).
        /// </para>
        /// <para>
        ///     If healing damage, this spreads the damage change proportional to the current damage value of each <see
        ///     cref="DamageTypePrototype"></see> (subject to integer rounding). If there is less damage than is being
        ///     healed, some healing is wasted. Unsupported damage types do not waste healing.
        /// </para> 
        /// </remarks>
        /// <param name="group">group of damage being changed.</param>
        /// <param name="amount">
        ///     Amount of damage being received (positive for damage, negative for heals).
        /// </param>
        /// <param name="ignoreDamageResistances">
        ///     Whether to ignore resistances when taking damage. Healing always ignores resistances, regardless of this
        ///     input.
        /// </param>
        /// <returns>
        ///     Returns false if the given group is not applicable or no damage change occurred; true otherwise.
        /// </returns>
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        bool ChangeDamage(
            DamageClass @class,
            int amount,
            bool ignoreResistances,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);

        /// <summary>
        ///     Forcefully sets the specified <see cref="DamageType"/> to the given
        ///     value, ignoring resistance values.
=======
        bool TryChangeDamage(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false);

        /// <summary>
=======
        bool TryChangeDamage(DamageGroupPrototype group, int amount, bool ignoreDamageResistances = false);

        /// <summary>
>>>>>>> refactor-damageablecomponent
        ///     Forcefully sets the specified <see cref="DamageTypePrototype"/> to the given value, ignoring resistance
        ///     values.
        /// </summary>
        /// <param name="type">Type of damage being set.</param>
<<<<<<< HEAD
        /// <param name="newValue">New damage value to be set.</param>
        /// <returns>
        ///     Returns false if a given type is not supported or a negative value is provided; true otherwise.
        /// </returns>
        bool TrySetDamage(DamageTypePrototype type, int newValue);

        /// <summary>
        ///     Forcefully sets all damage types in a specified damage group using <see cref="TrySetDamage"></see>.
        /// </summary>
        /// <remarks>
        ///     Note that the actual damage of this group will be equal to the given value times the number damage group
        ///     members that this container supports.
        /// </remarks>
        /// <param name="group">Group of damage being set.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <returns>
        ///     Returns false if the given group is not applicable or a negative value is provided; true otherwise.
        /// </returns>
        bool TrySetDamage(DamageGroupPrototype group, int newValue);

        /// <summary>
        ///     Sets all supported damage types to specified value using <see cref="TrySetDamage"></see>.
>>>>>>> Refactor damageablecomponent update (#4406)
        /// </summary>
        /// <param name="newValue">New damage value to be set.</param>
        /// <returns>
        ///     Returns false if a negative value is provided; true otherwise.
        /// </returns>
<<<<<<< refs/remotes/origin/master
        bool SetDamage(
            DamageType type,
            int newValue,
            IEntity? source = null,
            DamageChangeParams? extraParams = null);
=======
        bool TrySetAllDamage(int newValue);

        /// <summary>
        ///     Returns true if the given damage group is applicable to this damage container.
        /// </summary>
=======
        /// <param name="newValue">New damage value to be set.</param>
        /// <returns>
        ///     Returns false if a given type is not supported or a negative value is provided; true otherwise.
        /// </returns>
        bool TrySetDamage(DamageTypePrototype type, int newValue);

        /// <summary>
        ///     Forcefully sets all damage types in a specified damage group using <see cref="TrySetDamage"></see>.
        /// </summary>
        /// <remarks>
        ///     Note that the actual damage of this group will be equal to the given value times the number damage group
        ///     members that this container supports.
        /// </remarks>
        /// <param name="group">Group of damage being set.</param>
        /// <param name="newValue">New damage value to be set.</param>
        /// <returns>
        ///     Returns false if the given group is not applicable or a negative value is provided; true otherwise.
        /// </returns>
        bool TrySetDamage(DamageGroupPrototype group, int newValue);

        /// <summary>
        ///     Sets all supported damage types to specified value using <see cref="TrySetDamage"></see>.
        /// </summary>
        /// <param name="newValue">New damage value to be set.</param>
        /// <returns>
        ///     Returns false if a negative value is provided; true otherwise.
        /// </returns>
        bool TrySetAllDamage(int newValue);

        /// <summary>
        ///     Returns true if the given damage group is applicable to this damage container.
        /// </summary>
>>>>>>> refactor-damageablecomponent
        public bool IsApplicableDamageGroup(DamageGroupPrototype group);

        /// <summary>
        ///     Returns true if the given damage group is fully supported by this damage container.
        /// </summary>
        public bool IsFullySupportedDamageGroup(DamageGroupPrototype group);
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent

        /// <summary>
        ///     Returns true if the given damage type is supported by this damage container.
        /// </summary>
        public bool IsSupportedDamageType(DamageTypePrototype type);


        /// <summary>
        ///     Invokes the HealthChangedEvent with the current values of health.
        /// </summary>
        void ForceHealthChangedEvent();
    }
}
