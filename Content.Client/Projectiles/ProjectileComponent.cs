using Content.Shared.Projectiles;

namespace Content.Client.Projectiles
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public sealed class ProjectileComponent : SharedProjectileComponent {}
}
