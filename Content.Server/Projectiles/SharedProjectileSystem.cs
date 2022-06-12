using Content.Server.Administration.Logs;
using Content.Server.Projectiles.Components;
using Content.Server.Weapon.Melee;
using Content.Server.Weapon.Ranged;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Projectiles;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using GunSystem = Content.Server.Weapon.Ranged.Systems.GunSystem;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    public sealed class ProjectileSystem : SharedProjectileSystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;
        [Dependency] private readonly GunSystem _guns = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, ProjectileComponent component, StartCollideEvent args)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (args.OurFixture.ID != ProjectileFixture || !args.OtherFixture.Hard || component.DamagedEntity)
            {
                return;
            }

            var otherEntity = args.OtherFixture.Body.Owner;

            var modifiedDamage = _damageableSystem.TryChangeDamage(otherEntity, component.Damage);
            component.DamagedEntity = true;

            if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
            {
                _adminLogger.Add(LogType.BulletHit,
                    HasComp<ActorComponent>(otherEntity) ? LogImpact.Extreme : LogImpact.High,
                    $"Projectile {ToPrettyString(component.Owner):projectile} shot by {ToPrettyString(component.Shooter):user} hit {ToPrettyString(otherEntity):target} and dealt {modifiedDamage.Total:damage} damage");
            }

            _guns.PlayImpactSound(otherEntity, modifiedDamage, component.SoundHit, component.ForceSound);

            // Damaging it can delete it
            if (HasComp<CameraRecoilComponent>(otherEntity))
            {
                var direction = args.OurFixture.Body.LinearVelocity.Normalized;
                _cameraRecoil.KickCamera(otherEntity, direction);
            }

            if (component.DeleteOnCollide)
                QueueDel(uid);
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
