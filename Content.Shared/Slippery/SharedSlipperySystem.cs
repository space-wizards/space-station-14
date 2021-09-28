using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private List<SlipperyComponent> _slipped = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SlipperyComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, SlipperyComponent component, StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner.Uid;

            if (!CanSlip(component, otherUid, out _)) return;

            if (!_slipped.Contains(component))
                _slipped.Add(component);

            component.Colliding.Add(otherUid);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            for (var i = _slipped.Count - 1; i >= 0; i--)
            {
                var slipperyComp = _slipped[i];
                if (!Update(slipperyComp)) continue;
                _slipped.RemoveAt(i);
            }
        }

        public bool CanSlip(SlipperyComponent component, EntityUid uid, [NotNullWhen(true)] out SharedStunnableComponent? stunnableComponent)
        {
            if (!component.Slippery
                || component.Owner.IsInContainer()
                || component.Slipped.Contains(uid)
                || !EntityManager.TryGetComponent<SharedStunnableComponent>(uid, out stunnableComponent))
            {
                stunnableComponent = null;
                return false;
            }

            return true;
        }

        private bool TrySlip(SlipperyComponent component, IPhysBody ourBody, IPhysBody otherBody)
        {
            if (!CanSlip(component, otherBody.Owner.Uid, out var stun)) return false;

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

        private bool Update(SlipperyComponent component)
        {
            if (component.Deleted || !component.Slippery || component.Colliding.Count == 0)
                return true;

            if (!EntityManager.TryGetComponent(component.Owner.Uid, out PhysicsComponent? body))
            {
                component.Colliding.Clear();
                return true;
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

            return false;
        }
    }
}
