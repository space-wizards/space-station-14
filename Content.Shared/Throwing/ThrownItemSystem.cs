using System.Collections.Generic;
using System.Linq;
using Content.Shared.Collections;
using Content.Shared.Hands.Components;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var toRemove = new RemQueue<ThrownItemComponent>();

            // We can't just use sleeping unfortunately because there's a delay of the sleep timer. ThrownItemComponent
            // is transient while the entity is thrown so this shouldn't be too bad.
            foreach (var (thrown, physics) in ComponentManager.EntityQuery<ThrownItemComponent, PhysicsComponent>())
            {
                if (!physics.LinearVelocity.Equals(Vector2.Zero)) continue;
                toRemove.Add(thrown);
            }

            foreach (var comp in toRemove)
            {
                if (comp.Deleted) continue;
                LandComponent(comp);
            }
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
            LandComponent(thrownItem);
        }

        private void HandlePullStarted(PullStartedMessage message)
        {
            // TODO: this isn't directed so things have to be done the bad way
            if (message.Pulled.Owner.TryGetComponent(out ThrownItemComponent? thrownItem))
                LandComponent(thrownItem);
        }

        private void LandComponent(ThrownItemComponent thrownItem)
        {
            if (thrownItem.Owner.Deleted) return;

            var landing = thrownItem.Owner;

            if (!thrownItem.Owner.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            var fixture = physicsComponent.GetFixture(ThrowingFixture);

            if (fixture != null)
            {
                _broadphaseSystem.DestroyFixture(physicsComponent, fixture);
            }

            // Unfortunately we can't check for hands containers as they have specific names.
            if (thrownItem.Owner.TryGetContainerMan(out var containerManager) &&
                containerManager.Owner.HasComponent<SharedHandsComponent>())
            {
                ComponentManager.RemoveComponent(landing.Uid, thrownItem);
                return;
            }

            var user = thrownItem.Thrower;
            var coordinates = landing.Transform.Coordinates;

            var landMsg = new LandEvent(user, landing, coordinates);
            RaiseLocalEvent(landing.Uid, landMsg);
            ComponentManager.RemoveComponent(landing.Uid, thrownItem);
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
