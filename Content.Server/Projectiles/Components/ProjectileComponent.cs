using System.Collections.Generic;
using Content.Server.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Projectiles;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Projectiles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public class ProjectileComponent : SharedProjectileComponent, IStartCollide
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
        [DataField("soundHit")]
        private SoundSpecifier _soundHit = default!;

        private bool _damagedEntity;

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

        /// <summary>
        ///     Applies the damage when our projectile collides with its victim
        /// </summary>
        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (!otherFixture.Hard || _damagedEntity)
            {
                return;
            }

            var coordinates = otherFixture.Body.Owner.Transform.Coordinates;
            var playerFilter = Filter.Pvs(coordinates);
            if (otherFixture.Body.Owner.TryGetComponent(out IDamageableComponent? damage) && _soundHit.TryGetSound(out var soundHit))
            {
                SoundSystem.Play(playerFilter, soundHit, coordinates);
            }

            if (damage != null)
            {
                Owner.EntityManager.TryGetEntity(Shooter, out var shooter);

                foreach (var (damageType, amount) in _damages)
                {
                    damage.ChangeDamage(damageType, amount, false, shooter);
                }

                _damagedEntity = true;
            }

            // Damaging it can delete it
            if (!otherFixture.Body.Deleted && otherFixture.Body.Owner.TryGetComponent(out CameraRecoilComponent? recoilComponent))
            {
                var direction = ourFixture.Body.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }

            if(DeleteOnCollide)
                Owner.QueueDelete();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ProjectileComponentState(Shooter, IgnoreShooter);
        }
    }
}
