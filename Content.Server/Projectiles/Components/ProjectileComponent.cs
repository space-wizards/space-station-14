using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Projectiles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public class ProjectileComponent : SharedProjectileComponent
    {
        [DataField("damages")] private Dictionary<DamageType, int> _damages = new();

        [ViewVariables]
        public Dictionary<DamageType, int> Damages
        {
            get => _damages;
            set => _damages = value;
        }

        [DataField("deleteOnCollide")]
        public bool DeleteOnCollide { get; } = true;

        // Get that juicy FPS hit sound
        [DataField("soundHit")] public string? SoundHit = default;
        [DataField("soundHitSpecies")] public string? SoundHitSpecies = default;

        public bool DamagedEntity;

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            Shooter = shooter.Uid;
            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ProjectileComponentState(Shooter, IgnoreShooter);
        }
    }
}
