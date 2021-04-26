using System;
using System.ComponentModel.DataAnnotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage
{
    /// <summary>
    ///
    /// </summary>
    [Prototype("damageType")]
    [Serializable, NetSerializable]
    public class DamageTypePrototype : IPrototype
    {
        [DataField(tag: "id", required: true)]
        public string ID { get; } = default!;
    }
}
