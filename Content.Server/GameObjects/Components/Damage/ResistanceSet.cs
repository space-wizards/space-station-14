using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Resistance set used by damageable objects.
    /// For each damage type, has a coefficient, damage reduction and "included in total" value.
    /// </summary>
    public class ResistanceSet : IExposeData
    {
        public static ResistanceSet DefaultResistanceSet = new ResistanceSet();

        [ViewVariables]
        private readonly Dictionary<DamageType, ResistanceSetSettings> _resistances = new Dictionary<DamageType, ResistanceSetSettings>();

        public ResistanceSet()
        {
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                _resistances[damageType] = new ResistanceSetSettings();
            }
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                var resistanceName = damageType.ToString().ToLower();
                _resistances[damageType] = serializer.ReadDataField(resistanceName, new ResistanceSetSettings());
            } 
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

            return damageType != DamageType.Total && _resistances[damageType].AppliesToTotal;
        }

        /// <summary>
        /// Settings for a specific damage type in a resistance set.
        /// </summary>
        public class ResistanceSetSettings : IExposeData
        {
            public float Coefficient { get; private set; } = 1;
            public int DamageReduction { get; private set; } = 0;
            public bool AppliesToTotal { get; private set; } = true;

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(this, x => Coefficient, "coefficient", 1);
                serializer.DataField(this, x => DamageReduction, "damageReduction", 0);
                serializer.DataField(this, x => AppliesToTotal, "appliesToTotal", true);
            }
        }
    }
}
