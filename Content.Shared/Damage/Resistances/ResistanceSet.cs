using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Resistances
{
    /// <summary>
    ///     Set of resistances used by damageable objects.
    ///     Each <see cref="DamageTypePrototype"/> has a multiplier and flat damage
    ///     reduction value.
    /// </summary>
    [Serializable, NetSerializable]
    public class ResistanceSet
    {

        [ViewVariables]
        public string? ID { get; } = string.Empty;

        [ViewVariables]
        public Dictionary<DamageTypePrototype, ResistanceSetSettings> Resistances { get; } = new();

        public ResistanceSet()
        {
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            foreach (var damageType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                Resistances.Add(damageType, new ResistanceSetSettings(1f, 0));
            }
=======
>>>>>>> Merge fixes
=======
>>>>>>> refactor-damageablecomponent
        }

        public ResistanceSet(ResistanceSetPrototype data)
        {
            ID = data.ID;
            Resistances = data.Resistances;
        }

        /// <summary>
        ///     Adjusts input damage with the resistance set values.
        ///     Only applies reduction if the amount is damage (positive), not
        ///     healing (negative).
        /// </summary>
        /// <param name="damageType">Type of damage.</param>
        /// <param name="amount">Incoming amount of damage.</param>
        public int CalculateDamage(DamageTypePrototype damageType, int amount)
        {

            // Do nothing if the damage type is not specified in resistance set.
            if (!Resistances.TryGetValue(damageType, out var resistance))
            {
                return amount;
            }

            if (amount > 0) // Only apply reduction if it's healing, not damage.
            {
                amount -= resistance.FlatReduction;

                if (amount <= 0)
                {
                    return 0;
                }
            }

            amount = (int) Math.Ceiling(amount * resistance.Coefficient);

            return amount;
        }
    }
}
