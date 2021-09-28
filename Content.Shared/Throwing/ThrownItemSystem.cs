using System.Collections.Generic;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Handles throwing landing and collisions.
    /// </summary>
    public class ThrownItemSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        private const string ThrowingFixture = "throw-fixture";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrownItemComponent, PhysicsSleepMessage>(HandleSleep);
            SubscribeLocalEvent<ThrownItemComponent, StartCollideEvent>(HandleCollision);
            SubscribeLocalEvent<ThrownItemComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<ThrownItemComponent, ThrownEvent>(ThrowItem);
            SubscribeLocalEvent<PullStartedMessage>(HandlePullStarted);
        }

        private void ThrowItem(EntityUid uid, ThrownItemComponent component, ThrownEvent args)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent) ||
                physicsComponent.Fixtures.Count != 1) return;

            if (physicsComponent.GetFixture(ThrowingFixture) != null)
            {
                Logger.Error($"Found existing throwing fixture on {component.Owner}");
                return;
            }

            var shape = physicsComponent.Fixtures[0].Shape;
            _broadphaseSystem.CreateFixture(physicsComponent, new Fixture(physicsComponent, shape) {CollisionLayer = (int) CollisionGroup.ThrownItem, Hard = false, ID = ThrowingFixture});
        }

        private void HandleCollision(EntityUid uid, ThrownItemComponent component, StartCollideEvent args)
        {
            var thrower = component.Thrower;
            var otherBody = args.OtherFixture.Body;

            if (otherBody.Owner == thrower) return;
            ThrowCollideInteraction(thrower, args.OurFixture.Body, otherBody);
        }

        private void PreventCollision(EntityUid uid, ThrownItemComponent component, PreventCollideEvent args)
        {
            if (args.BodyB.Owner == component.Thrower)
            {
                args.Cancel();
            }
        }

        private void HandleSleep(EntityUid uid, ThrownItemComponent thrownItem, PhysicsSleepMessage message)
        {
            StopThrow(uid, thrownItem);
        }

        private void HandlePullStarted(PullStartedMessage message)
        {
            // TODO: this isn't directed so things have to be done the bad way
            if (EntityManager.TryGetComponent(message.Pulled.Owner.Uid, out ThrownItemComponent? thrownItemComponent))
                StopThrow(message.Pulled.Owner.Uid, thrownItemComponent);
        }

        private void StopThrow(EntityUid uid, ThrownItemComponent thrownItemComponent)
        {
            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                var fixture = physicsComponent.GetFixture(ThrowingFixture);

                if (fixture != null)
                {
                    _broadphaseSystem.DestroyFixture(physicsComponent, fixture);
                }
            }

            EntityManager.EventBus.RaiseLocalEvent(uid, new StopThrowEvent {User = thrownItemComponent.Thrower?.Uid});
            EntityManager.RemoveComponent<ThrownItemComponent>(uid);
        }

        public void LandComponent(ThrownItemComponent thrownItem)
        {
            if (thrownItem.Deleted || thrownItem.Owner.Deleted || _containerSystem.IsEntityInContainer(thrownItem.Owner.Uid)) return;

            var landing = thrownItem.Owner;

            // Unfortunately we can't check for hands containers as they have specific names.
            if (thrownItem.Owner.TryGetContainerMan(out var containerManager) &&
                containerManager.Owner.HasComponent<SharedHandsComponent>())
            {
                EntityManager.RemoveComponent(landing.Uid, thrownItem);
                return;
            }

            var landMsg = new LandEvent {User = thrownItem.Thrower?.Uid};
            RaiseLocalEvent(landing.Uid, landMsg, false);
        }

        /// <summary>
        ///     Raises collision events on the thrown and target entities.
        /// </summary>
        public void ThrowCollideInteraction(IEntity? user, IPhysBody thrown, IPhysBody target)
        {
            // TODO: Just pass in the bodies directly
            RaiseLocalEvent(target.Owner.Uid, new ThrowHitByEvent(user, thrown.Owner, target.Owner));
            RaiseLocalEvent(thrown.Owner.Uid, new ThrowDoHitEvent(user, thrown.Owner, target.Owner));
        }
    }
}
