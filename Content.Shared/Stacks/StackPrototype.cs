#nullable enable
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stacks
{
    [Prototype("stack")]
    public class StackPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("icon")]
        public SpriteSpecifier? Icon { get; }

        /// <summary>
        ///     The entity id that will be spawned by default from this stack.
        /// </summary>
        [DataField("spawn")]
        public string? Spawn { get; }
    }
}
