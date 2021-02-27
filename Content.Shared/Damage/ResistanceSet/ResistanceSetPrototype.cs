using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.ResistanceSet
{
    /// <summary>
    ///     Prototype for the BodyPart class.
    /// </summary>
    [Prototype("resistanceSet")]
    [Serializable, NetSerializable]
    public class ResistanceSetPrototype : IPrototype, ISerializationHooks
    {
        [DataField("coefficients")]
        private Dictionary<DamageType, float> _coefficients;

        [DataField("flatReductions")]
        private Dictionary<DamageType, int> _flatReductions;

        [ViewVariables] public Dictionary<DamageType, float> Coefficients => _coefficients;

        [ViewVariables] public Dictionary<DamageType, int> FlatReductions => _flatReductions;

        [ViewVariables] public Dictionary<DamageType, ResistanceSetSettings> Resistances { get; private set; }

        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string Parent { get; }

        public void AfterDeserialization()
        {
            Resistances = new Dictionary<DamageType, ResistanceSetSettings>();
            foreach (var damageType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                Resistances.Add(damageType,
                    new ResistanceSetSettings(_coefficients[damageType], _flatReductions[damageType]));
            }
        }
    }
}
