#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Physics.Pull;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrownItemComponent, PhysicsSleepMessage>(HandleSleep);
            SubscribeLocalEvent<PullStartedMessage>(HandlePullStarted);
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
