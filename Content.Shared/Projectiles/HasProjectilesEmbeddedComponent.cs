using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

/// <summary>
/// This component marks that its entity has projectiles embedded in it, and tracks what those projectile entities are.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HasProjectilesEmbeddedComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> EmbeddedProjectiles = [];
}
