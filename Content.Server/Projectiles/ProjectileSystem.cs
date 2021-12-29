using Content.Server.Administration.Logs;
using Content.Server.Projectiles.Components;
using Content.Shared.Body.Components;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    internal sealed class ProjectileSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;
        [Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;

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

            var coordinates = EntityManager.GetComponent<TransformComponent>(args.OtherFixture.Body.Owner).Coordinates;
            var playerFilter = Filter.Pvs(coordinates);

            if (!EntityManager.GetComponent<MetaDataComponent>(otherEntity).EntityDeleted && component.SoundHitSpecies != null &&
                EntityManager.HasComponent<SharedBodyComponent>(otherEntity))
            {
                SoundSystem.Play(playerFilter, component.SoundHitSpecies.GetSound(), coordinates);
            }
            else
            {
                var soundHit = component.SoundHit?.GetSound();

                if (!string.IsNullOrEmpty(soundHit))
                    SoundSystem.Play(playerFilter, soundHit, coordinates);
            }

            if (!EntityManager.GetComponent<MetaDataComponent>(otherEntity).EntityDeleted)
            {
                var dmg = _damageableSystem.TryChangeDamage(otherEntity, component.Damage);
                component.DamagedEntity = true;

                if (dmg is not null && EntityManager.EntityExists(component.Shooter))
                    _adminLogSystem.Add(LogType.BulletHit,
                        HasComp<ActorComponent>(otherEntity) ? LogImpact.Extreme : LogImpact.High,
                        $"Projectile {ToPrettyString(component.Owner):projectile} shot by {ToPrettyString(component.Shooter):user} hit {ToPrettyString(otherEntity):target} and dealt {dmg.Total:damage} damage");
            }

            // Damaging it can delete it
            if (!EntityManager.GetComponent<MetaDataComponent>(otherEntity).EntityDeleted &&
                EntityManager.HasComponent<CameraRecoilComponent>(otherEntity))
            {
                var direction = args.OurFixture.Body.LinearVelocity.Normalized;
                _cameraRecoil.KickCamera(otherEntity, direction);
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
                    EntityManager.DeleteEntity(component.Owner);
                }
            }
        }
    }
}
