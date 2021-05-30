#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Physics.Pull;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    ///     Handles throwing landing and collisions.
    /// </summary>
    public class ThrownItemSystem : EntitySystem
    {
        private List<IThrowCollide> _throwCollide = new();

        private const string ThrowingFixture = "throw-fixture";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrownItemComponent, PhysicsSleepMessage>(HandleSleep);
            SubscribeLocalEvent<ThrownItemComponent, StartCollideEvent>(HandleCollision);
            SubscribeLocalEvent<ThrownItemComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<ThrownItemComponent, ThrownEvent>(ThrowItem);
            SubscribeLocalEvent<ThrownItemComponent, LandEvent>(LandItem);
            SubscribeLocalEvent<PullStartedMessage>(HandlePullStarted);
        }

        private void LandItem(EntityUid uid, ThrownItemComponent component, LandEvent args)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            var fixture = physicsComponent.GetFixture(ThrowingFixture);
            if (fixture == null)
            {
                Logger.Error($"Tried to remove throwing fixture for {component.Owner} but none found?");
                return;
            }
            physicsComponent.RemoveFixture(fixture);
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
            var fixture = new Fixture(physicsComponent, shape) {CollisionLayer = (int) CollisionGroup.ThrownItem, Hard = false, Name = ThrowingFixture};
            physicsComponent.AddFixture(fixture);
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

            var user = thrownItem.Thrower;
            var landing = thrownItem.Owner;
            var coordinates = landing.Transform.Coordinates;

            // LandInteraction
            // TODO: Refactor these to system messages
            var landMsg = new LandEvent(user, landing, coordinates);
            RaiseLocalEvent(landMsg);
            if (landMsg.Handled)
            {
                return;
            }

            var comps = landing.GetAllComponents<ILand>().ToArray();
            var landArgs = new LandEventArgs(user, coordinates);

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                if (landing.Deleted) break;
                comp.Land(landArgs);
            }

            ComponentManager.RemoveComponent(landing.Uid, thrownItem);
        }

        /// <summary>
        ///     Calls ThrowCollide on all components that implement the IThrowCollide interface
        ///     on a thrown entity and the target entity it hit.
        /// </summary>
        public void ThrowCollideInteraction(IEntity? user, IPhysBody thrown, IPhysBody target)
        {
            // TODO: Just pass in the bodies directly
            var collideMsg = new ThrowCollideEvent(user, thrown.Owner, target.Owner);
            RaiseLocalEvent(collideMsg);
            if (collideMsg.Handled)
            {
                return;
            }

            var eventArgs = new ThrowCollideEventArgs(user, thrown.Owner, target.Owner);

            foreach (var comp in target.Owner.GetAllComponents<IThrowCollide>())
            {
                _throwCollide.Add(comp);
            }

            foreach (var collide in _throwCollide)
            {
                if (target.Owner.Deleted) break;
                collide.HitBy(eventArgs);
            }

            _throwCollide.Clear();

            foreach (var comp in thrown.Owner.GetAllComponents<IThrowCollide>())
            {
                _throwCollide.Add(comp);
            }

            foreach (var collide in _throwCollide)
            {
                if (thrown.Owner.Deleted) break;
                collide.DoHit(eventArgs);
            }

            _throwCollide.Clear();
        }
    }
}
