using Content.Server.Camera;
using Content.Server.Projectiles.Components;
using Content.Shared.Damage.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    internal sealed class ProjectileSystem : EntitySystem
    {
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

            var coordinates = args.OtherFixture.Body.Owner.Transform.Coordinates;
            var playerFilter = Filter.Pvs(coordinates);
            if (args.OtherFixture.Body.Owner.TryGetComponent(out IDamageableComponent? damage) && component.SoundHitSpecies != null)
            {
                SoundSystem.Play(playerFilter, component.SoundHitSpecies, coordinates);
            }
            else if (component.SoundHit != null)
            {
                SoundSystem.Play(playerFilter, component.SoundHit, coordinates);
            }

            if (damage != null)
            {
                EntityManager.TryGetEntity(component.Shooter, out var shooter);

                foreach (var (damageType, amount) in component.Damages)
                {
                    damage.ChangeDamage(damageType, amount, false, shooter);
                }

                component.DamagedEntity = true;
            }

            // Damaging it can delete it
            if (!args.OtherFixture.Body.Deleted && args.OtherFixture.Body.Owner.TryGetComponent(out CameraRecoilComponent? recoilComponent))
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
