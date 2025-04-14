using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SetSelector;

/// <summary>
/// A prototype that defines a set available for selection for <see>
///     <cref>SetSelectorComponent</cref>
/// </see>
/// </summary>
[Prototype]
public sealed class SelectableSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
    [DataField] public List<EntProtoId> Content = new();
}
