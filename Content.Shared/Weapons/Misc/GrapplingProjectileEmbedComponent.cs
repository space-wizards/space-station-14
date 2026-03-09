using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

/// <summary>
/// Component used to track that an object has a grappling projectile embeded into it, to ensure joint relays between grid and entities are properly updated.
/// SLAM-TODO: List needs to be cleaned up when a grappling projectile is deleted...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrapplingProjectileEmbedComponent : Component
{
    /// <summary>
    /// The projectiles embedded in this entity.
    /// </summary>
    [DataField]
    public List<EntityUid> GrapplingProjectiles = new();
}
