using Content.Server.Administration.Logs;
using Content.Server.Camera;
using Content.Server.Projectiles.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
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
        [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;

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

            if (!((!IoCManager.Resolve<IEntityManager>().EntityExists(otherEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(otherEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted) && component.SoundHitSpecies != null &&
                IoCManager.Resolve<IEntityManager>().HasComponent<SharedBodyComponent>(otherEntity.Uid))
            {
                SoundSystem.Play(playerFilter, component.SoundHitSpecies.GetSound(), coordinates);
            }
            else
            {
                var soundHit = component.SoundHit?.GetSound();

                if (!string.IsNullOrEmpty(soundHit))
                    SoundSystem.Play(playerFilter, soundHit, coordinates);
            }

            if (!((!IoCManager.Resolve<IEntityManager>().EntityExists(otherEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(otherEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted))
            {
                var dmg = _damageableSystem.TryChangeDamage(otherEntity.Uid, component.Damage);
                component.DamagedEntity = true;

                if (dmg is not null && EntityManager.TryGetEntity(component.Shooter, out var shooter))
                    _adminLogSystem.Add(LogType.BulletHit, LogImpact.Low,
                        $"Projectile {component.Owner} shot by {shooter} hit {otherEntity} and dealt {dmg.Total} damage");
            }

            // Damaging it can delete it
            if (!((!IoCManager.Resolve<IEntityManager>().EntityExists(otherEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(otherEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted) && IoCManager.Resolve<IEntityManager>().TryGetComponent(otherEntity.Uid, out CameraRecoilComponent? recoilComponent))
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
                    IoCManager.Resolve<IEntityManager>().DeleteEntity(component.Owner.Uid);
                }
            }
        }
    }
}
