using Content.Server.Administration.Logs;
using Content.Server.Projectiles.Components;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Projectiles;
using Content.Shared.Vehicle.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using GunSystem = Content.Server.Weapon.Ranged.Systems.GunSystem;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    public sealed class ProjectileSystem : SharedProjectileSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly GunSystem _guns = default!;
        [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<ProjectileComponent, ComponentGetState>(OnGetState);
        }

        private void OnGetState(EntityUid uid, ProjectileComponent component, ref ComponentGetState args)
        {
            args.State = new ProjectileComponentState(component.Shooter, component.IgnoreShooter);
        }

        private void OnStartCollide(EntityUid uid, ProjectileComponent component, StartCollideEvent args)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (args.OurFixture.ID != ProjectileFixture || !args.OtherFixture.Hard || component.DamagedEntity)
                return;

            // Relay the projectile to the vehicle owner.
            // See note in shared about other relays.
            var otherEntity = args.OtherFixture.Body.Owner;

            if (TryComp<VehicleComponent>(otherEntity, out var vehicle) && vehicle.Rider != null)
                otherEntity = vehicle.Rider.Value;

            var direction = args.OurFixture.Body.LinearVelocity.Normalized;

            Collide(component, otherEntity, direction);
        }

        private void Collide(ProjectileComponent component, EntityUid otherEntity, Vector2 direction)
        {
            var modifiedDamage = _damageableSystem.TryChangeDamage(otherEntity, component.Damage, component.IgnoreResistances);
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
                _sharedCameraRecoil.KickCamera(otherEntity, direction);
            }

            if (component.DeleteOnCollide)
            {
                QueueDel(component.Owner);

                if (component.ImpactEffect != null && TryComp<TransformComponent>(component.Owner, out var xform))
                {
                    RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, xform.Coordinates));
                }
            }
        }
    }
}
