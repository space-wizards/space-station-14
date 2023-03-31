using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Afterlight.ThirdDimension;

/// <summary>
/// This is used for ladders and traversing them.
/// </summary>
[RegisterComponent]
public sealed class LadderComponent : Component
{
    [DataField("primary")]
    public bool Primary = false;

    [DataField("otherHalf")]
    public EntityUid? OtherHalf = EntityUid.Invalid;

    [DataField("otherHalfProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string OtherHalfProto = "LadderLower";
}
