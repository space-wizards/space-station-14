using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed class ForceWallSpellComponent : Component
{
    /// <summary>
    /// Timer for accumulation before the force wall is deleted.
    /// </summary>
    [ViewVariables]
    public float Timer = 0f;

    /// <summary>
    /// How long the force walls are active for.
    /// </summary>
    [ViewVariables]
    [DataField("forceWallCooldown")]
    public float ForceWallCooldown = 20f;
}
