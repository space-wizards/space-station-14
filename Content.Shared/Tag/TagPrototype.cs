using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Tag
{
    /// <summary>
    ///     Prototype representing a tag in YAML.
    ///     Meant to only have an ID property, as that is the only thing that
    ///     gets saved in TagComponent.
    /// </summary>
    [Prototype("Tag")]
    public sealed class TagPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;
    }
}
