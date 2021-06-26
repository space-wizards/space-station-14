using System;
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
        [DataField("id", required: true)]
        public string ID { get; } = default!;
    }
}
