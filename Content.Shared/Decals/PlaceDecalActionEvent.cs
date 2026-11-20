using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Decals;

public sealed partial class PlaceDecalActionEvent : WorldTargetActionEvent
{
    [DataField(required: true)]
    public ProtoId<DecalPrototype> DecalId;

    [DataField]
    public Color Color;

    [DataField]
    public double Rotation;

    [DataField]
    public bool Snap;

    [DataField]
    public int ZIndex;

    [DataField]
    public bool Cleanable;
}
