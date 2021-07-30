using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage
{
    /// <summary>
    /// A Group of <see cref="DamageTypePrototype"/>s .
    /// </summary>
    [Prototype("damageGroup")]
    [Serializable, NetSerializable]
    public class DamageGroupPrototype : IPrototype, ISerializationHooks
    {
        private IPrototypeManager _prototypeManager = default!;

        [DataField("id", required: true)] public string ID { get; } = default!;

        [DataField("damageTypes", required: true)]
        public List<string> TypeIds { get; } = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        }

        public IEnumerable<DamageTypePrototype> DamageTypes
        {
            get
            {
                foreach (var ID in TypeIds)
                {
                    var typeResolved = _prototypeManager.Index<DamageTypePrototype>(ID);
                    yield return typeResolved;
                }
            }
        }
    }
}
