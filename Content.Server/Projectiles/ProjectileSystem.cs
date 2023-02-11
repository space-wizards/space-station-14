using Content.Server.Administration.Logs;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Physics.Events;
using Content.Server.Hands.Components;
using Robust.Shared.Random;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Shared.Physics.Systems;
using Content.Shared.Audio;

namespace Content.Server.Projectiles
{
    [UsedImplicitly]
    public sealed class ProjectileSystem : SharedProjectileSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly GunSystem _guns = default!;
        [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

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

        private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (args.OurFixture.ID != ProjectileFixture || !args.OtherFixture.Hard || component.DamagedEntity)
                return;

            var otherEntity = args.OtherFixture.Body.Owner;
            var otherName = ToPrettyString(otherEntity);

            if (TryComp<HandsComponent>(otherEntity, out var hands))
            {
                foreach (var (_, hand) in hands.Hands)
                {
                    if (TryComp<ReflectProjectileComponent>(hand.HeldEntity, out var reflect) 
                        && reflect.Enabled
                        && _random.Prob(reflect.ReflectChance))
                    {
                        var vel = _physics.GetMapLinearVelocity(uid);
                        var force = args.OurFixture.Body.Force;
                        _physics.ResetDynamics(args.OurFixture.Body);
                        _physics.ApplyForce(uid, -force);
                        _physics.SetLinearVelocity(uid, -vel);
                        component.Shooter = otherEntity;
                        _popup.PopupEntity(Loc.GetString("reflect-projectile"), uid, PopupType.Small);
                        _audio.PlayPvs(reflect.OnReflect, uid, AudioHelpers.WithVariation(0.05f, _random));
                        return;
                    }
                }
            }

            var direction = args.OurFixture.Body.LinearVelocity.Normalized;
            var modifiedDamage = _damageableSystem.TryChangeDamage(otherEntity, component.Damage, component.IgnoreResistances, origin: component.Shooter);
            component.DamagedEntity = true;
            var deleted = Deleted(otherEntity);

            if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
            {
                if (modifiedDamage.Total > FixedPoint2.Zero && !deleted)
                {
                    RaiseNetworkEvent(new DamageEffectEvent(Color.Red, new List<EntityUid> {otherEntity}), Filter.Pvs(otherEntity, entityManager: EntityManager));
                }

                _adminLogger.Add(LogType.BulletHit,
                    HasComp<ActorComponent>(otherEntity) ? LogImpact.Extreme : LogImpact.High,
                    $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter):user} hit {otherName:target} and dealt {modifiedDamage.Total:damage} damage");
            }

            if (!deleted)
            {
                _guns.PlayImpactSound(otherEntity, modifiedDamage, component.SoundHit, component.ForceSound);
                _sharedCameraRecoil.KickCamera(otherEntity, direction);
            }

            if (component.DeleteOnCollide)
            {
                QueueDel(uid);

                if (component.ImpactEffect != null && TryComp<TransformComponent>(component.Owner, out var xform))
                {
                    RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, xform.Coordinates), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
                }
            }
        }
    }
}
