using System.Threading;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Mono.Cecil;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class EmitterComponent : PowerConsumerComponent, IInteractHand
    {
        [Dependency] private IEntityManager _entityManager;

        public override string Name => "Emitter";

        private CancellationTokenSource tokenSource;

        public bool IsPowered = false;

        private PhysicsComponent _collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("EmitterComponent created with no CollidableComponent");
                return;
            }
            _collidableComponent.AnchoredChanged += OnAnchoredChanged;
        }

        private void OnAnchoredChanged()
        {
            if(_collidableComponent.Anchored) Owner.SnapToGrid();
        }

        public void PowerOn()
        {
            IsPowered = true;

            DrawRate = 500;

            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Timer.SpawnRepeating(1000,  () =>
            {
                if (!Fire())
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

            var projectile = _entityManager.SpawnEntity("EmitterBolt", Owner.Transform.Coordinates);

            if (!projectile.TryGetComponent<PhysicsComponent>(out var physicsComponent))
            {
                Logger.Error("Emitter tried firing a bolt, but it was spawned without a CollidableComponent");
                return false;
            }
            physicsComponent.Status = BodyStatus.InAir;

            if (!projectile.TryGetComponent<ProjectileComponent>(out var projectileComponent))
            {
                Logger.Error("Emitter tried firing a bolt, but it was spawned without a ProjectileComponent");
                return false;
            }
            projectileComponent.IgnoreEntity(Owner);

            physicsComponent
                .EnsureController<BulletController>()
                .LinearVelocity = Owner.Transform.WorldRotation.ToVec() * 20f;

            projectile.Transform.LocalRotation = Owner.Transform.WorldRotation;

            Timer.Spawn(3000, () => projectile.Delete());

            return true;
        }
    }
}
