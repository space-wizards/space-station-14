#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using Robust.Shared.IoC;
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
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        [field: DataField("coefficients")]
        public Dictionary<string, float> Coefficients { get; } = new();

        [ViewVariables]
        [field: DataField("flatReductions")]
        public Dictionary<string, int> FlatReductions { get; } = new();

        [ViewVariables]
        public Dictionary<DamageTypePrototype, float> Resistances { get; private set; } = new();
        [ViewVariables]
        public Dictionary<DamageTypePrototype, int> FlatResistances { get; private set; } = new();

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            Resistances = new Dictionary<DamageTypePrototype, float>();
            FlatResistances = new Dictionary<DamageTypePrototype, int>();

            foreach (var KeyValuePair in Coefficients)
            {
                var resolvedDamageType = _prototypeManager.Index<DamageTypePrototype>(KeyValuePair.Key);
                Resistances.Add(resolvedDamageType,KeyValuePair.Value);
            }

            foreach (var KeyValuePair in FlatReductions)
            {
                var resolvedDamageType = _prototypeManager.Index<DamageTypePrototype>(KeyValuePair.Key);
                Resistances.Add(resolvedDamageType,KeyValuePair.Value);
            }

            foreach (var damageType in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
            {
               // Resistances.Add(damageType, new ResistanceSetSettings(Coefficients[damageType], FlatReductions[damageType]));
            }
        }
    }
}
