using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Thief;

/// <summary>
/// TODO
/// </summary>
[Prototype("thiefBackpackSet")]
public sealed partial class ThiefBackpackSetPrototype : IPrototype
{
    // ID
    [IdDataField]
    public string ID { get; private set; } = default!;

    // Name
    [DataField]
    public string Name { get; private set; } = string.Empty;

    // Description
    [DataField]
    public string Description { get; private set; } = string.Empty;

    // Sprite
    [DataField]
    public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    //Item in set
    [DataField]
    public List<ProtoId<EntityPrototype>> Content = new();
}
