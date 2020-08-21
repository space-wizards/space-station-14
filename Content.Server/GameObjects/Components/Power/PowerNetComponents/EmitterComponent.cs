using System.Threading;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Mono.Cecil;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class EmitterComponent : PowerConsumerComponent, IInteractHand
    {
        public override string Name => "Emitter";

        private IEntityManager _entityManager;

        private CancellationTokenSource tokenSource;

        public bool IsPowered = false;

        public override void Initialize()
        {
            base.Initialize();

            _entityManager = IoCManager.Resolve<IEntityManager>();

            tokenSource = new CancellationTokenSource();

        }

        public void PowerOn()
        {
            IsPowered = true;

            DrawRate = 500;

            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            bool didFire;

            Timer.SpawnRepeating(1000,  () =>
            {
                didFire = Fire();
                if (!didFire)
                {
                    PowerOff();
                }

            }, token);

        }

        public void PowerOff()
        {
            IsPowered = false;

            DrawRate = 0;

            tokenSource.Cancel();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!IsPowered)
            {
                PowerOn();
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The emitter turns on."));
            }
            else
            {
                PowerOff();
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The emitter turns off."));
            }

            return true;
        }

        public bool Fire()
        {
            if (DrawRate > ReceivedPower)
            {
                return false;
            }

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

            return true;
        }
    }
}
