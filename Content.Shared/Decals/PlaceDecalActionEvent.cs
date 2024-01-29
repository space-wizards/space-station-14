using Content.Shared.Actions;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Decals;

public sealed partial class PlaceDecalActionEvent : WorldTargetActionEvent
{
    [DataField("decalId", customTypeSerializer:typeof(PrototypeIdSerializer<DecalPrototype>), required:true)]
    public string DecalId = string.Empty;

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
