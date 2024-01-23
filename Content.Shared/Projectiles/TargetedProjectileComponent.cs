using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent]
public sealed partial class TargetedProjectileComponent : Component
{
    /// <summary>
    ///     The entity this projectile was aimed at.
    /// </summary>
    [DataField]
    public EntityUid? Target;
}
