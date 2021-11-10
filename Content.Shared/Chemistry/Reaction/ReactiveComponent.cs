using System;
using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Chemistry.Reaction
{
    [RegisterComponent]
    public class ReactiveComponent : Component
    {
        public override string Name => "Reactive";

        [DataField("reactions", required: true, readOnly: true, serverOnly: true)]
        public List<ReactiveReagentEffectEntry> Reactions { get; } = default!;
    }

    public class ReactiveReagentEffectEntry
    {
        [DataField("methods")]
        public HashSet<ReactionMethod> Methods = default!;

        [DataField("reagents")]
        public HashSet<string>? Reagents = null;

        [DataField("effects", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<ReagentPrototype>))]
        public List<ReagentEffect> Effects = default!;
    }
}
