using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Thief;

/// <summary>
/// A prototype that defines a set of items and visuals in a specific starter set for the antagonist thief
/// </summary>
[Prototype("thiefBackpackSet")]
public sealed partial class ThiefBackpackSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    [DataField] public List<EntProtoId> Content = new();
}
