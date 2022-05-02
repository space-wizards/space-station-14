using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed class ForceWallComponent : Component
{
    [ViewVariables]
    [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototype = "WallCult";

    [ViewVariables]
    public float Timer = 0f;

    [ViewVariables]
    public float ForceWallCooldown = 30f;
}
