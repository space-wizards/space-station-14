using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Robust.Shared.Audio;

namespace Content.Server.Projectiles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public sealed class ProjectileComponent : SharedProjectileComponent
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("deleteOnCollide")]
        public bool DeleteOnCollide { get; } = true;

        [DataField("ignoreResistances")]
        public bool IgnoreResistances { get; } = false;

        // Get that juicy FPS hit sound
        [DataField("soundHit")] public SoundSpecifier? SoundHit;

        [DataField("soundForce")]
        public bool ForceSound = false;

        public bool DamagedEntity;
    }
}
