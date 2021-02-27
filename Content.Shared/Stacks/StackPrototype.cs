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

        [ViewVariables]
        [DataField("parent")]
        public string? Parent { get; }

        public string Name { get; } = string.Empty;

        public SpriteSpecifier? Icon { get; private set; }

        /// <summary>
        ///     The entity id that will be spawned by default from this stack.
        /// </summary>
        public string? Spawn { get; private set; }
    }
}
