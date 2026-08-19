using Robust.Shared.GameStates;

namespace Content.Shared.Mapping;

/// <summary>
/// This entity can be aligned by StructureAlignerSystem
/// When being Aligned, it will look for adjacent StructureAlignToComponents with the matching AlignType,
/// and rotate to be in-line with them.
/// See StructureAlignerSystem.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StructureAlignerComponent : Component
{
    /// <summary>
    /// The type that this entity will align to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public StructureAlignType AlignType = StructureAlignType.Door;

    /// <summary>
    /// Align the entity when it gets anchored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AnchorAlign;
}
