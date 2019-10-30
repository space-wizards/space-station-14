using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Resistance set used by damageable objects.
    /// For each damage type, has a coefficient, damage reduction and "included in total" value.
    /// </summary>
    public class ResistanceSet
    {
        Dictionary<DamageType, ResistanceSetSettings> _resistances = new Dictionary<DamageType, ResistanceSetSettings>();
        static Dictionary<string, ResistanceSet> _resistanceSets = new Dictionary<string, ResistanceSet>();

        //TODO: make it load from YAML instead of hardcoded like this
        public ResistanceSet()
        {
            _resistances.Add(DamageType.Total, new ResistanceSetSettings(1f, 0, true));
            _resistances.Add(DamageType.Acid, new ResistanceSetSettings(1f, 0, true));
            _resistances.Add(DamageType.Brute, new ResistanceSetSettings(1f, 0, true));
            _resistances.Add(DamageType.Heat, new ResistanceSetSettings(1f, 0, true));
            _resistances.Add(DamageType.Cold, new ResistanceSetSettings(1f, 0, true));
            _resistances.Add(DamageType.Toxic, new ResistanceSetSettings(1f, 0, true));
            _resistances.Add(DamageType.Electric, new ResistanceSetSettings(1f, 0, true));
        }

        /// <summary>
        /// Loads a resistance set with the given name.
        /// </summary>
        /// <param name="setName">Name of the resistance set.</param>
        /// <returns>Resistance set by given name</returns>
        public static ResistanceSet GetResistanceSet(string setName)
        {
            ResistanceSet resistanceSet = null;

            if (!_resistanceSets.TryGetValue(setName, out resistanceSet))
            {
                resistanceSet = Load(setName);
            }

            return resistanceSet;
        }

        static ResistanceSet Load(string setName)
        {
            //TODO: only creates a standard set RN, should be YAMLed

            ResistanceSet resistanceSet = new ResistanceSet();

            _resistanceSets.Add(setName, resistanceSet);

            return resistanceSet;
        }

        /// <summary>
        /// Adjusts input damage with the resistance set values.
        /// </summary>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="amount">Incoming amount of the damage.</param>
        /// <returns>Damage adjusted by the resistance set.</returns>
        public int CalculateDamage(DamageType damageType, int amount)
        {
            if (amount > 0) //if it's damage, reduction applies
            {
                amount -= _resistances[damageType].DamageReduction;

                if (amount <= 0)
                    return 0;
            }

            amount = (int)Math.Floor(amount * _resistances[damageType].Coefficient);

            return amount;
        }

        public bool AppliesToTotal(DamageType damageType)
        {
            //Damage that goes straight to total (for whatever reason) never applies twice

            return damageType == DamageType.Total ? false : _resistances[damageType].AppliesToTotal;
        }

        /// <summary>
        /// Settings for a specific damage type in a resistance set.
        /// </summary>
        struct ResistanceSetSettings
        {
            public float Coefficient { get; private set; }
            public int DamageReduction { get; private set; }
            public bool AppliesToTotal { get; private set; }

            public ResistanceSetSettings(float coefficient, int damageReduction, bool appliesInTotal)
            {
                Coefficient = coefficient;
                DamageReduction = damageReduction;
                AppliesToTotal = appliesInTotal;
            }
        }
    }
}
