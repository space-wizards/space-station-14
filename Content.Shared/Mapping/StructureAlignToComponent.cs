using Robust.Shared.GameStates;

namespace Content.Shared.Mapping;

/// <summary>
/// This entity can determine the alignment of adjacent entities that are being Aligned.
/// See StructureAlignerSystem.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StructureAlignToComponent : Component
{
    /// <summary>
    /// The types that will align to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<StructureAlignType> AlignType = new () { StructureAlignType.Door };
}
