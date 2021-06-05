using System;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class ReactiveComponent : Component
    {
        public override string Name => "Reactive";

        [DataField("reactions", true, serverOnly:true)]
        public ReagentEntityReaction[] Reactions { get; } = Array.Empty<ReagentEntityReaction>();
    }
}
