<<<<<<< refs/remotes/origin/master
<<<<<<< refs/remotes/origin/master
﻿using System;
=======
﻿#nullable enable
using System;
using System.CodeDom;
>>>>>>> update damagecomponent across shared and server
=======
using System;
using System.CodeDom;
>>>>>>> Refactor damageablecomponent update (#4406)
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
<<<<<<< refs/remotes/origin/master
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
=======
        [DataField("id", required: true)]
        public string ID { get; } = default!;
>>>>>>> update damagecomponent across shared and server

        [ViewVariables]
        [DataField("coefficients", required: true)]
        private Dictionary<string, float> coefficients { get; } = new();

        [ViewVariables]
        [DataField("flatReductions", required: true)]
        private Dictionary<string, int> flatReductions { get; } = new();

        [ViewVariables]
<<<<<<< refs/remotes/origin/master
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
=======
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
>>>>>>> update damagecomponent across shared and server
        }
    }

}
