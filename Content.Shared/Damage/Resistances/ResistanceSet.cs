#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Resistances
{
    /// <summary>
    ///     Set of resistances used by damageable objects.
    ///     Each <see cref="DamageType"/> has a multiplier and flat damage
    ///     reduction value.
    /// </summary>
    [NetSerializable]
    [Serializable]
    public class ResistanceSet
    {
        [ViewVariables]
        private Dictionary<DamageType, ResistanceSetSettings> _resistances =
            new();

        public ResistanceSet()
        {
            foreach (var damageType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                _resistances.Add(damageType, new ResistanceSetSettings(1f, 0));
            }
        }

        public ResistanceSet(ResistanceSetPrototype data)
        {
            ID = data.ID;
            _resistances = data.Resistances;
        }

        public string ID { get; } = string.Empty;

        /// <summary>
        ///     Adjusts input damage with the resistance set values.
        ///     Only applies reduction if the amount is damage (positive), not
        ///     healing (negative).
        /// </summary>
        /// <param name="damageType">Type of damage.</param>
        /// <param name="amount">Incoming amount of damage.</param>
        public int CalculateDamage(DamageType damageType, int amount)
        {
            if (amount > 0) // Only apply reduction if it's healing, not damage.
            {
                amount -= _resistances[damageType].FlatReduction;

                if (amount <= 0)
                {
                    return 0;
                }
            }

            amount = (int) Math.Ceiling(amount * _resistances[damageType].Coefficient);

            return amount;
        }
    }

    /// <summary>
    ///     Settings for a specific damage type in a resistance set. Flat reduction is applied before the coefficient.
    /// </summary>
    [Serializable, NetSerializable]
    public struct ResistanceSetSettings
    {
        [ViewVariables] public float Coefficient { get; private set; }

        [ViewVariables] public int FlatReduction { get; private set; }

        public ResistanceSetSettings(float coefficient, int flatReduction)
        {
            Coefficient = coefficient;
            FlatReduction = flatReduction;
        }
    }
}
