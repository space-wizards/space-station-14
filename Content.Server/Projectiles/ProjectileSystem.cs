using Content.Server.Camera;
using Content.Server.Projectiles.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Content.Shared.Damage;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    internal sealed class ProjectileSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, ProjectileComponent component, StartCollideEvent args)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (!args.OtherFixture.Hard || component.DamagedEntity)
            {
                return;
            }

            var otherEntity = args.OtherFixture.Body.Owner;

            var coordinates = args.OtherFixture.Body.Owner.Transform.Coordinates;
            var playerFilter = Filter.Pvs(coordinates);

            if (!otherEntity.Deleted && component.SoundHitSpecies != null &&
                otherEntity.HasComponent<SharedBodyComponent>())
            {
                SoundSystem.Play(playerFilter, component.SoundHitSpecies.GetSound(), coordinates);
            }
            else
            {
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
                var soundHit = component.SoundHit?.GetSound();

                if (!string.IsNullOrEmpty(soundHit))
                    SoundSystem.Play(playerFilter, soundHit, coordinates);
=======
                SoundSystem.Play(playerFilter, component.SoundHit.GetSound(), coordinates);
>>>>>>> Bring refactor-damageablecomponent branch up-to-date with master (#4510)
=======
                SoundSystem.Play(playerFilter, component.SoundHit.GetSound(), coordinates);
>>>>>>> refactor-damageablecomponent
            }

            if (!otherEntity.Deleted && otherEntity.TryGetComponent(out IDamageableComponent? damage))
            {
                EntityManager.TryGetEntity(component.Shooter, out var shooter);

                foreach (var (damageTypeID, amount) in component.Damages)
                {
                    damage.TryChangeDamage(_prototypeManager.Index<DamageTypePrototype>(damageTypeID), amount);
                }

                component.DamagedEntity = true;
            }

            // Damaging it can delete it
            if (!otherEntity.Deleted && otherEntity.TryGetComponent(out CameraRecoilComponent? recoilComponent))
            {
                var direction = args.OurFixture.Body.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }

            if (component.DeleteOnCollide)
                EntityManager.QueueDeleteEntity(uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<ProjectileComponent>())
            {
                component.TimeLeft -= frameTime;

                if (component.TimeLeft <= 0)
                {
                    component.Owner.Delete();
                }
            }
        }
    }
}
