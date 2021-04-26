using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.Core.Tokens;

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

        [field: DataField(tag: "id", required: true)]
        public string ID { get; } = default!;

        [field: DataField(tag: "damageTypes", required: true)]
        public List<string> TypeIds { get; } = default!;

        public IEnumerable<DamageTypePrototype> Types = default!;

        public void AfterSerialization()
        {
            foreach (var typeid in TypeIds)
            {
                Types = Types.Concat(new []{_prototypeManager.Index<DamageTypePrototype>(typeid)});
            }
        }

    }
}
