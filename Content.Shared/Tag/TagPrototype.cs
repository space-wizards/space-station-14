using Robust.Shared.Prototypes;

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
        [IdDataField]
        public string ID { get; } = default!;
    }
}
