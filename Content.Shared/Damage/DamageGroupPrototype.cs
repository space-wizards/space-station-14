using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage
{
    /// <summary>
    ///
    /// </summary>
    [Prototype("damageGroup")]
    [Serializable, NetSerializable]
    public class DamageGroupPrototype : IPrototype, ISerializationHooks
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("damageTypes", required: true)]
        public List<string> TypeIds { get; } = default!;

        public IEnumerable<DamageTypePrototype> DamageTypes = default!;

        public void AfterSerialization()
        {
            foreach (var typeid in TypeIds)
            {
                DamageTypes = DamageTypes.Concat(new []{_prototypeManager.Index<DamageTypePrototype>(typeid)});
            }
        }
    }
}
