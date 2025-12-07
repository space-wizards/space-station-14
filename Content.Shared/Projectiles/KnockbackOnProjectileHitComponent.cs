using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class KnockbackOnProjectileHitComponent : Component
{
    /// <summary>
    /// Distance in tiles the target will be knocked back.
    /// </summary>
    [DataField("distance"), ViewVariables(VVAccess.ReadWrite)]
    public float Distance = 4f;

    /// <summary>
    /// Throw speed used by the throwing system (affects how fast the target is pushed).
    /// </summary>
    [DataField("speed"), ViewVariables(VVAccess.ReadWrite)]
    public float Speed = 10f;

    /// <summary>
    /// Whether the knockback should unanchor the target if anchorable.
    /// </summary>
    [DataField("unanchor"), ViewVariables(VVAccess.ReadWrite)]
    public bool Unanchor = false;
}
