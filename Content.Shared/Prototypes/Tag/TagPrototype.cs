#nullable enable
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Prototypes.Tag
{
    /// <summary>
    ///     Prototype representing a tag in YAML.
    ///     Meant to only have an ID property, as that is the only thing that
    ///     gets saved in TagComponent.
    /// </summary>
    [Prototype("Tag")]
    public class TagPrototype : IPrototype, IIndexedPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; [UsedImplicitly] private set; } = string.Empty;
    }
}
