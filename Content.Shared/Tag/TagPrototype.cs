using Robust.Shared.Prototypes;

namespace Content.Shared.Tag;

/// <summary>
/// Prototype representing a tag in YAML.
/// Meant to only have an ID property, as that is the only thing that
/// gets saved in TagComponent.
/// </summary>
[Prototype("Tag")]
public sealed partial class TagPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = string.Empty;
}
