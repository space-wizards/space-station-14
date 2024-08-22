using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.Prototypes;

[Prototype("paintableGroup")]
public sealed partial class PaintableGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Category = String.Empty;

    [DataField]
    public PaintableVisuals Visuals = PaintableVisuals.BaseRSI;

    [DataField]
    public string? State = null;

    [DataField]
    public float Time = 2.0f;

    [DataField(required: true)]
    public Dictionary<string, EntProtoId> StylePaths = new();

    [DataField]
    public bool Duplicates = false;

    // The priority determines, which sprite is used when showing
    // the icon for a style in the SprayPainter UI. The highest priority
    // gets shown.
    [DataField]
    public int IconPriority = 0;
}

[Serializable, NetSerializable]
public enum PaintableVisuals
{
    BaseRSI,
    LockerRSI,
}
