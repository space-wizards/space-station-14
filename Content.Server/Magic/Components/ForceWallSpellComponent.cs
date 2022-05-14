using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed class ForceWallSpellComponent : Component
{
    /// <summary>
    /// How long the force walls are active for.
    /// </summary>
    [ViewVariables]
    [DataField("lifetime")]
    public float Lifetime = 20f;
}
