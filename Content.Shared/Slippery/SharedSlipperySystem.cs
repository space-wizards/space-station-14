using System.Linq;
using Content.Shared.EffectBlocker;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Slippery
{
    [UsedImplicitly]
    public abstract class SharedSlipperySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SlipperyComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, SlipperyComponent component, StartCollideEvent args)
        {
            component.Colliding.Add(args.OtherFixture.Body.Owner.Uid);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var slipperyComp in ComponentManager.EntityQuery<SlipperyComponent>().ToArray())
            {
                Update(slipperyComp);
            }
        }

        private bool TrySlip(SlipperyComponent component, IPhysBody ourBody, IPhysBody otherBody)
        {
            if (!component.Slippery
                || component.Owner.IsInContainer()
                ||  component.Slipped.Contains(otherBody.Owner.Uid)
                ||  !otherBody.Owner.TryGetComponent(out SharedStunnableComponent? stun))
            {
                return false;
            }

            if (otherBody.LinearVelocity.Length < component.RequiredSlipSpeed || stun.KnockedDown)
            {
                return false;
            }

            var percentage = otherBody.GetWorldAABB().IntersectPercentage(ourBody.GetWorldAABB());

            if (percentage < component.IntersectPercentage)
            {
                return false;
            }

            if (!EffectBlockerSystem.CanSlip(otherBody.Owner))
            {
                return false;
            }

            otherBody.LinearVelocity *= component.LaunchForwardsMultiplier;

            stun.Paralyze(5);
            component.Slipped.Add(otherBody.Owner.Uid);
            component.Dirty();

            PlaySound(component);

            return true;
        }

        // Until we get predicted slip sounds TM?
        protected abstract void PlaySound(SlipperyComponent component);

        // TODO: Now that we have StartCollide and EndCollide this should just use that to track bodies intersecting.
        private void Update(SlipperyComponent component)
        {
            if (!component.Slippery)
                return;

            if (!ComponentManager.TryGetComponent(component.Owner.Uid, out PhysicsComponent? body))
            {
                component.Colliding.Clear();
                return;
            }

            foreach (var uid in component.Colliding.ToArray())
            {
                if (!uid.IsValid() || !EntityManager.TryGetEntity(uid, out var entity))
                {
                    component.Colliding.Remove(uid);
                    component.Slipped.Remove(uid);
                    component.Dirty();
                    continue;
                }

                if (!entity.TryGetComponent(out PhysicsComponent? otherPhysics) ||
                    !body.GetWorldAABB().Intersects(otherPhysics.GetWorldAABB()))
                {
                    component.Colliding.Remove(uid);
                    component.Slipped.Remove(uid);
                    component.Dirty();
                    continue;
                }

                if (!component.Slipped.Contains(uid))
                    TrySlip(component, body, otherPhysics);
            }
        }
    }
}
