using System;
using System.Collections.Generic;
<<<<<<< refs/remotes/origin/master
=======
using Robust.Shared.IoC;
>>>>>>> Merge fixes
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Resistances
{
    /// <summary>
    ///     Prototype for the BodyPart class.
    /// </summary>
    [Prototype("resistanceSet")]
    [Serializable, NetSerializable]
    public class ResistanceSetPrototype : IPrototype, ISerializationHooks
    {
        [ViewVariables]
        [DataField("coefficients")]
<<<<<<< refs/remotes/origin/master
        public Dictionary<DamageType, float> Coefficients { get; } = new();

        [ViewVariables]
        [DataField("flatReductions")]
        public Dictionary<DamageType, int> FlatReductions { get; } = new();

        [ViewVariables]
        public Dictionary<DamageType, ResistanceSetSettings> Resistances { get; private set; } = new();
=======
        public Dictionary<string, float> Coefficients { get; } = new();

        [ViewVariables]
        [DataField("flatReductions")]
        public Dictionary<string, int> FlatReductions { get; } = new();

        [ViewVariables]
        public Dictionary<DamageTypePrototype, float> Resistances { get; private set; } = new();

        [ViewVariables]
        public Dictionary<DamageTypePrototype, int> FlatResistances { get; private set; } = new();
>>>>>>> Merge fixes

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            Resistances = new Dictionary<DamageType, ResistanceSetSettings>();
            foreach (var damageType in (DamageType[]) Enum.GetValues(typeof(DamageType)))
            {
                Resistances.Add(damageType,
                    new ResistanceSetSettings(Coefficients[damageType], FlatReductions[damageType]));
            }
        }
    }
}
