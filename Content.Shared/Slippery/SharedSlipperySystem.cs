using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Slippery
{
    [UsedImplicitly]
    public abstract class SharedSlipperySystem : EntitySystem
    {
        [Dependency] private readonly SharedAdminLogSystem _adminLog = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

        private readonly List<SlipperyComponent> _slipped = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<SlipperyComponent, StartCollideEvent>(HandleCollide);
            SubscribeLocalEvent<NoSlipComponent, SlipAttemptEvent>(OnNoSlipAttempt);
        }

        private void HandleCollide(EntityUid uid, SlipperyComponent component, StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner;

            if (!CanSlip(component, otherUid)) return;

            if (!_slipped.Contains(component))
                _slipped.Add(component);

            component.Colliding.Add(otherUid);
        }

        private void OnNoSlipAttempt(EntityUid uid, NoSlipComponent component, SlipAttemptEvent args)
        {
            args.Cancel();
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

        public bool CanSlip(SlipperyComponent component, EntityUid uid)
        {
            if (!component.Slippery
                || component.Owner.IsInContainer()
                || component.Slipped.Contains(uid)
                || !_statusEffectsSystem.CanApplyEffect(uid, "Stun")) //Should be KnockedDown instead?
            {
                return false;
            }

            return true;
        }

        private bool TrySlip(SlipperyComponent component, IPhysBody ourBody, IPhysBody otherBody)
        {
            if (!CanSlip(component, otherBody.Owner)) return false;

            if (otherBody.LinearVelocity.Length < component.RequiredSlipSpeed)
            {
                return false;
            }

            var percentage = otherBody.GetWorldAABB().IntersectPercentage(ourBody.GetWorldAABB());

            if (percentage < component.IntersectPercentage)
            {
                return false;
            }

            var ev = new SlipAttemptEvent();
            RaiseLocalEvent(otherBody.Owner, ev, false);
            if (ev.Cancelled)
                return false;

            otherBody.LinearVelocity *= component.LaunchForwardsMultiplier;

            bool playSound = !_statusEffectsSystem.HasStatusEffect(otherBody.Owner, "KnockedDown");

            _stunSystem.TryParalyze(otherBody.Owner, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            component.Slipped.Add(otherBody.Owner);
            component.Dirty();

            //Preventing from playing the slip sound when you are already knocked down.
            if(playSound)
            {
                PlaySound(component);
            }

            _adminLog.Add(LogType.Slip, LogImpact.Low, $"{ToPrettyString(otherBody.Owner):mob} slipped on collision with {ToPrettyString(component.Owner):entity}");

            return true;
        }

        // Until we get predicted slip sounds TM?
        protected abstract void PlaySound(SlipperyComponent component);

        private bool Update(SlipperyComponent component)
        {
            if (component.Deleted || !component.Slippery || component.Colliding.Count == 0)
                return true;

            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? body))
            {
                component.Colliding.Clear();
                return true;
            }

            foreach (var uid in component.Colliding.ToArray())
            {
                if (!uid.IsValid())
                {
                    component.Colliding.Remove(uid);
                    component.Slipped.Remove(uid);
                    component.Dirty();
                    continue;
                }

                if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? otherPhysics) ||
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

    /// <summary>
    ///     Raised on an entity to determine if it can slip or not.
    /// </summary>
    public class SlipAttemptEvent : CancellableEntityEventArgs
    {
    }
}
