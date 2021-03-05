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
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string? Parent { get; }

        [field: DataField("name")]
        public string Name { get; } = string.Empty;

        [field: DataField("icon")]
        public SpriteSpecifier? Icon { get; }

        /// <summary>
        ///     The entity id that will be spawned by default from this stack.
        /// </summary>
        [field: DataField("spawn")]
        public string? Spawn { get; }
    }
}
