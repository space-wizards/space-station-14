using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

/// <summary>
/// Component used to track that an object has a grappling projectile embeded into it, to ensure joint relays between grid and entities are properly updated.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrapplingProjectileEmbedComponent : Component
{
    /// <summary>
    /// The projectiles embedded in this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> GrapplingProjectiles = new();
}
