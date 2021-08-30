using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     Prototype of damage resistance sets. Can be applied to <see cref="DamageSpecifier"/> using <see
    ///     cref="DamageSpecifier.ApplyResistanceSet(ResistanceSetPrototype)"/>. This can be done several times as the
    ///     <see cref="DamageSpecifier"/> is passed to it's final target. By default the receiving <see cref="DamageableComponent"/>, will
    ///     also apply it's own <see cref="ResistanceSetPrototype"/>.
    /// </summary>
    [Prototype("resistanceSet")]
    [Serializable, NetSerializable]
    public class ResistanceSetPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("coefficients", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
        public Dictionary<string, float> Coefficients = new();

        [DataField("flatReductions", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, DamageTypePrototype>))]
        public Dictionary<string, float> FlatReduction = new();
    }
}
