using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.Prototypes;

/// <summary>
/// Contains a map of the objects from which the spray painter will take texture to paint another from the same group.
/// </summary>
[Prototype]
public sealed partial class PaintableGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The field is responsible for in which tab of the spray painter menu the group will be displayed.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PaintableGroupCategoryPrototype> Category;

    /// <summary>
    /// What appearance data must be sent when this is repainted.
    /// </summary>
    [DataField]
    public PaintableVisuals Visuals = PaintableVisuals.BaseRSI;

    /// <summary>
    /// The time required to paint an object from a given group.
    /// </summary>
    [DataField]
    public float Time = 2.0f;

    /// <summary>
    /// Map from localization keys and entity identifiers displayed in the spray painter menu.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Styles = new();

    /// <summary>
    /// If multiple groups have the same key, the group with the highest IconPriority has its icon displayed.
    /// </summary>
    [DataField]
    public int IconPriority;
}

[Serializable, NetSerializable]
[Flags]
public enum PaintableVisuals
{
    BaseRSI,
    Prototype,
}
