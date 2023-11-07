using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives;

/// <summary>
/// General data about a group of items, such as icon, description, name. Used for Steal objective
/// </summary>
[Prototype("stealTargetGroup")]
public sealed partial class StealTargetGroupPrototype : IPrototype
{
    /// ID
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// Name
    [DataField]
    public string Name { get; private set; } = string.Empty;

    // Sprite
    [DataField]
    public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;
}
