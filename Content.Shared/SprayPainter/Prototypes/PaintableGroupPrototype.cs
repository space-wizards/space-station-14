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
    /// The time required to paint an object from a given group, in seconds.
    /// </summary>
    [DataField]
    public float Time = 2.0f;

    /// <summary>
    /// To number of charges needed to paint an object of this group.
    /// </summary>
    [DataField]
    public int Cost = 1;

    /// <summary>
    /// The default style to start painting.
    /// </summary>
    [DataField(required: true)]
    public string DefaultStyle = default!;

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
public enum PaintableVisuals
{
    /// <summary>
    /// The prototype to base the object's visuals off.
    /// </summary>
    Prototype
}
