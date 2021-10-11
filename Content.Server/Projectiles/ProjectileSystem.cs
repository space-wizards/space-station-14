using Content.Server.Camera;
using Content.Server.Projectiles.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    internal sealed class ProjectileSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

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
                var soundHit = component.SoundHit?.GetSound();

                if (!string.IsNullOrEmpty(soundHit))
                    SoundSystem.Play(playerFilter, soundHit, coordinates);
            }

            if (!otherEntity.Deleted)
            {
                _damageableSystem.TryChangeDamage(otherEntity.Uid, component.Damage);
                component.DamagedEntity = true;
                // "DamagedEntity" is misleading. Hit entity may be more accurate, as the damage may have been resisted
                // by resistance sets.
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

            foreach (var component in EntityManager.EntityQuery<ProjectileComponent>())
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
