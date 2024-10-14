using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

/// <summary>
/// This component marks that its entity has projectiles embedded in it, and tracks what those projectile entities are.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HasProjectilesEmbeddedComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]

    private List<EntityUid> _embeddedProjectiles = [];

    public void Add(Entity<EmbeddableProjectileComponent> projectile) => _embeddedProjectiles.Add(projectile);
    public bool Contains(Entity<EmbeddableProjectileComponent> projectile) => _embeddedProjectiles.Contains(projectile);
    public void Remove(Entity<EmbeddableProjectileComponent> projectile) => _embeddedProjectiles.Remove(projectile);
    public bool IsEmpty() => _embeddedProjectiles.Count == 0;
}
