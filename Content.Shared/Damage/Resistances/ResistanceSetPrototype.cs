using System;
using System.CodeDom;
using System.Collections.Generic;
using Robust.Shared.IoC;
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
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("coefficients", required: true)]
        private Dictionary<string, float> coefficients { get; } = new();

        [ViewVariables]
        [DataField("flatReductions", required: true)]
        private Dictionary<string, int> flatReductions { get; } = new();

        [ViewVariables]
        public Dictionary<DamageTypePrototype, ResistanceSetSettings> Resistances { get; private set; } = new();

        void ISerializationHooks.AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (var damageTypeID in coefficients.Keys)
            {
                var resolvedDamageType = prototypeManager.Index<DamageTypePrototype>(damageTypeID);
                Resistances.Add(resolvedDamageType, new ResistanceSetSettings(coefficients[damageTypeID], flatReductions[damageTypeID]));
            }
        }
    }

    /// <summary>
    ///   Resistance Settings for a specific DamageType. Flat reduction should always be applied before the coefficient.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct ResistanceSetSettings
    {
        [ViewVariables] public readonly float Coefficient;
        [ViewVariables] public readonly int FlatReduction;

        public ResistanceSetSettings(float coefficient, int flatReduction)
        {
            Coefficient = coefficient;
            FlatReduction = flatReduction;
        }
    }

}
