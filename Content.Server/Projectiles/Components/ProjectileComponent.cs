using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Projectiles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public class ProjectileComponent : SharedProjectileComponent
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("deleteOnCollide")]
        public bool DeleteOnCollide { get; } = true;

        // Get that juicy FPS hit sound
        [DataField("soundHit", required: true)] public SoundSpecifier? SoundHit = default!;
        [DataField("soundHitSpecies")] public SoundSpecifier? SoundHitSpecies = null;

        public bool DamagedEntity;

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(EntityUid shooter)
        {
            Shooter = shooter;
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new ProjectileComponentState(Shooter, IgnoreShooter);
        }
    }
}
