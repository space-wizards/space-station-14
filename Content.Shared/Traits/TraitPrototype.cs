using Robust.Shared.Prototypes;
using static Robust.Shared.Prototypes.EntityPrototype; // don't worry about it

namespace Content.Shared.Traits
{
    /// <summary>
    ///     Describes a trait.
    /// </summary>
    [Prototype("trait")]
    public sealed class TraitPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this trait.
        /// </summary>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     The description of this trait.
        /// </summary>
        [DataField("description")]
        public string? Description { get; }

        /// <summary>
        ///     The components that get added to the player, when they pick this trait.
        /// </summary>
        [DataField("components")]
        public ComponentRegistry Components { get; } = default!;
    }
}
