using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Chemistry.Reaction
{
    [RegisterComponent]
    public class ReactiveComponent : Component
    {
        public override string Name => "Reactive";

        [DataField("groups", required: true, readOnly: true, serverOnly: true,
            customTypeSerializer:typeof(PrototypeIdDictionarySerializer<HashSet<ReactionMethod>, ReactiveGroupPrototype>))]
        public Dictionary<string, HashSet<ReactionMethod>> ReactiveGroups { get; } = default!;
    }
}
