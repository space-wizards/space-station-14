using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Decals;

public sealed partial class PlaceDecalActionEvent : WorldTargetActionEvent
{
    [DataField(required:true)]
    public ProtoId<DecalPrototype> DecalId;

    [DataField("color")]
    public Color Color;

    [DataField("rotation")]
    public double Rotation;

    [DataField("snap")]
    public bool Snap;

    [DataField("zIndex")]
    public int ZIndex;

    [DataField("cleanable")]
    public bool Cleanable;
}
