using System.Threading;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.Physics;
using Mono.Cecil;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class EmitterComponent : PowerConsumerComponent
    {
        public override string Name => "Emitter";

        private IEntityManager _entityManager;

        public override void Initialize()
        {
            base.Initialize();

            _entityManager = IoCManager.Resolve<IEntityManager>();

            Timer.SpawnRepeating(1000, Fire, CancellationToken.None);
        }

        public void Fire()
        {
            var projectile = _entityManager.SpawnEntity("EmitterBolt", Owner.Transform.GridPosition);

            var physicsComponent = projectile.GetComponent<ICollidableComponent>();
            physicsComponent.Status = BodyStatus.InAir;

            var projectileComponent = projectile.GetComponent<ProjectileComponent>();
            projectileComponent.IgnoreEntity(Owner);

            projectile
                .GetComponent<ICollidableComponent>()
                .EnsureController<BulletController>()
                .LinearVelocity = Owner.Transform.WorldRotation.ToVec() * 20f;

            projectile.Transform.LocalRotation = Owner.Transform.WorldRotation;
        }
    }
}
