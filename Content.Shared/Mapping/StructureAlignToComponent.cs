namespace Content.Shared.Mapping;

/// <summary>
/// This entity can determine the alignment of adjacent entities that are being Aligned.
/// See StructureAlignerSystem.
/// </summary>
[RegisterComponent]
public sealed partial class StructureAlignToComponent : Component
{
    /// <summary>
    /// The types that will align to this entity.
    /// </summary>
    [DataField]
    public List<StructureAlignType> AlignType = new () { StructureAlignType.Door };
}
