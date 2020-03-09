using Content.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes
{
    /// <summary>
    /// Resistance set used by damageable objects.
    /// For each damage type, has a coefficient, damage reduction and "included in total" value.
    /// </summary>
    [Serializable, NetSerializable, Prototype("resistanceset")]
    public class ResistanceSetPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        [ViewVariables]
        public string ID => _id;
        [ViewVariables]
        Dictionary<DamageType, ResistanceSetSettings> _resistances = new Dictionary<DamageType, ResistanceSetSettings>();

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);

            //default ResistanceSetPrototype
            foreach (DamageType damagetype in Enum.GetValues(typeof(DamageType)))
            {
                _resistances.Add(damagetype, new ResistanceSetSettings(1, 0, true));
            }

            mapping.TryGetNode<YamlSequenceNode>("resistancesets", out var resistancesequencenode);
            var resistancemappingnode = resistancesequencenode.Cast<YamlMappingNode>();
            foreach (var resistanceMap in resistancemappingnode)
            {
                //defaults setting values
                var coefficient = 1f;
                var damred = 0;
                var appliestototal = true;

                //get YAML values
                if (resistanceMap.TryGetNode("coefficient", out var coeffnode))
                {
                    coefficient = float.Parse((string) coeffnode);
                }
                if (resistanceMap.TryGetNode("damagereduction", out var damrednode))
                {
                    damred = int.Parse((string) damrednode);
                }
                if (resistanceMap.TryGetNode("appliestototal", out var appliestototalnode))
                {
                    appliestototal = bool.Parse((string) appliestototalnode);
                }

                //adds value set to prototype
                resistanceMap.TryGetNode("damagetype", out var damagetypenode);
                var damagetype = (DamageType) Enum.Parse(typeof(DamageType), (string) damagetypenode);
                _resistances[damagetype] = new ResistanceSetSettings(coefficient, damred, appliestototal);
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

            amount = (int) Math.Floor(amount * _resistances[damageType].Coefficient);

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
        [Serializable, NetSerializable]
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
