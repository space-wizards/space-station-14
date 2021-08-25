using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     Prototype of damage resistance sets. Can be applied to <see cref="DamageData"/> using <see
    ///     cref="DamageData.ApplyResistanceSet(ResistanceSetPrototype)"/>. This can be done several times as the
    ///     <see cref="DamageData"/> is passed to it's final target. By default the receiving <see cref="DamageableComponent"/>, will
    ///     also apply it's own <see cref="ResistanceSetPrototype"/>.
    /// </summary>
    [Prototype("resistanceSet")]
    [Serializable, NetSerializable]
    public class ResistanceSetPrototype : IPrototype, ISerializationHooks
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        // TODO PROTOTYPE REFERENCES replace these two data-fields with prototype references and remove  ISerializationHooks.AfterDeserialization().
        [DataField("coefficients")]
        private Dictionary<string, float> _coefficientIDs  = new();

        [DataField("flatReductions")]
        private Dictionary<string, float> _flatReductionIDs = new();


        /// <summary>
        ///     Value subtracted from damage, before multiplying by coefficient.
        /// </summary>
        /// <remarks>
        ///     Positive values will decrease damage taken, and vice versa. Damage reduction never results in healing.
        ///     Even though damage is counted in integers, this can be a float, as it may make a difference when paired with the coefficient.
        /// </remarks>
        [ViewVariables]
        public  Dictionary<DamageTypePrototype, float> FlatReduction { get; } = new();
        // using float rather than int, because when combined with coefficients it might make a difference;

        /// <summary>
        ///     Multiplicative coefficient that modifies damage, after having subtracted the flat damage adjustment.
        /// </summary>
        /// <remarks>
        ///     Negative values will turn damage into healing. Maybe useful in some instances, like fire-elementals being healed by heat?
        ///     It's either a feature or I'm too lazy to add checks to stop you from putting negative coefficients in the yaml, you decide.
        ///     Will not turn healing into damage, because resistances don't apply to healing.
        /// </remarks>
        [ViewVariables]
        public Dictionary<DamageTypePrototype, float> Coefficients { get; } = new();

        void ISerializationHooks.AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            // Resolve coefficients
            foreach (var damageTypeID in _coefficientIDs.Keys)
            {
                if (prototypeManager.TryIndex<DamageTypePrototype>(damageTypeID, out var damageType))
                {
                    Coefficients.Add(damageType, _coefficientIDs[damageTypeID]);
                }
            }

            // Resolve flat reduction
            foreach (var damageTypeID in _flatReductionIDs.Keys)
            {
                if (prototypeManager.TryIndex<DamageTypePrototype>(damageTypeID, out var damageType))
                {
                    FlatReduction.Add(damageType, _flatReductionIDs[damageTypeID]);
                }
            }
        }
    }
}
