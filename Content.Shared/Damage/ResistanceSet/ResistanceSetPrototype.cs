#nullable enable
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
        [ViewVariables]
        [field: DataField("coefficients")]
        public Dictionary<DamageType, float> Coefficients { get; } = new();

        [ViewVariables]
        [field: DataField("flatReductions")]
        public Dictionary<DamageType, int> FlatReductions { get; } = new();

        [ViewVariables]
        public Dictionary<DamageType, ResistanceSetSettings> Resistances { get; private set; } = new();

        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string? Parent { get; }

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
