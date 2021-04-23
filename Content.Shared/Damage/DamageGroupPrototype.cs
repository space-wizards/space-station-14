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
    public class DamageGroupPrototype : IPrototype, ISerializationHooks
    {
        [Dependency]
        private readonly IPrototypeManager _prototypeManager = default!;

        [field: DataField(tag: "id", required: true)]
        public string ID { get; } = default!;

        [field: DataField(tag: "name", required: true)]
        public string Name { get; } = default!;

        [field: DataField(tag: "types", required: true )]
        public ImmutableList<string> TypeIds { get; } = ImmutableList<string>.Empty;

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
