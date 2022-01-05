using System;
using System.Collections.Generic;
using Content.Shared.Pulling;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    public class PullController : VirtualController
    {
        // Parameterization for pulling:
        // Speeds. Note that the speed is mass-independent (multiplied by mass).
        // Instead, tuning to mass is done via the mass values below.
        // Note that setting the speed too high results in overshoots (stabilized by drag, but bad)
        private const float AccelModifierHigh = 15f;
        private const float AccelModifierLow = 60.0f;
        // High/low-mass marks. Curve is constant-lerp-constant, i.e. if you can even pull an item,
        // you'll always get at least AccelModifierLow and no more than AccelModifierHigh.
        private const float AccelModifierHighMass = 70.0f; // roundstart saltern emergency closet
        private const float AccelModifierLowMass = 5.0f; // roundstart saltern emergency crowbar
        // Used to control settling (turns off pulling).
        private const float MaximumSettleVelocity = 0.1f;
        private const float MaximumSettleDistance = 0.01f;
        // Settle shutdown control.
        // Mustn't be too massive, as that causes severe mispredicts *and can prevent it ever resolving*.
        // Exists to bleed off "I pulled my crowbar" overshoots.
        // Minimum velocity for shutdown to be necessary. This prevents stuff getting stuck b/c too much shutdown.
        private const float SettleMinimumShutdownVelocity = 0.25f;
        // Distance in which settle shutdown multiplier is at 0. It then scales upwards linearly with closer distances.
        private const float SettleShutdownDistance = 1.0f;
        // Velocity change of -LinearVelocity * frameTime * this
        private const float SettleShutdownMultiplier = 20.0f;

        private SharedPullingSystem _pullableSystem = default!;

        public override List<Type> UpdatesAfter => new() {typeof(MoverController)};

        public override void Initialize()
        {
            base.Initialize();

            _pullableSystem = EntitySystem.Get<SharedPullingSystem>();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var pullable in _pullableSystem.Moving)
            {
                // There's a 1-frame delay between stopping moving something and it leaving the Moving set.
                // This can include if leaving the Moving set due to not being pulled anymore,
                //  or due to being deleted.

                if (pullable.Deleted)
                {
                    continue;
                }

                if (pullable.MovingTo == null)
                {
                    continue;
                }

                if (pullable.Puller is not {Valid: true} puller)
                {
                    continue;
                }

                // Now that's over with...

                var pullerPosition = EntityManager.GetComponent<TransformComponent>(puller).MapPosition;
                var movingTo = pullable.MovingTo.Value.ToMap(EntityManager);
                if (movingTo.MapId != pullerPosition.MapId)
                {
                    _pullableSystem.StopMoveTo(pullable);
                    continue;
                }

                if (!EntityManager.TryGetComponent<PhysicsComponent?>(pullable.Owner, out var physics) ||
                    physics.BodyType == BodyType.Static ||
                    movingTo.MapId != EntityManager.GetComponent<TransformComponent>(pullable.Owner).MapID)
                {
                    _pullableSystem.StopMoveTo(pullable);
                    continue;
                }

                var movingPosition = movingTo.Position;
                var ownerPosition = EntityManager.GetComponent<TransformComponent>(pullable.Owner).MapPosition.Position;

                var diff = movingPosition - ownerPosition;
                var diffLength = diff.Length;

                if ((diffLength < MaximumSettleDistance) && (physics.LinearVelocity.Length < MaximumSettleVelocity))
                {
                    physics.LinearVelocity = Vector2.Zero;
                    _pullableSystem.StopMoveTo(pullable);
                    continue;
                }

                var impulseModifierLerp = Math.Min(1.0f, Math.Max(0.0f, (physics.Mass - AccelModifierLowMass) / (AccelModifierHighMass - AccelModifierLowMass)));
                var impulseModifier = MathHelper.Lerp(AccelModifierLow, AccelModifierHigh, impulseModifierLerp);
                var multiplier = diffLength < 1 ? impulseModifier * diffLength : impulseModifier;
                // Note the implication that the real rules of physics don't apply to pulling control.
                var accel = diff.Normalized * multiplier;
                // Now for the part where velocity gets shutdown...
                if ((diffLength < SettleShutdownDistance) && (physics.LinearVelocity.Length >= SettleMinimumShutdownVelocity))
                {
                    // Shutdown velocity increases as we get closer to centre
                    var scaling = (SettleShutdownDistance - diffLength) / SettleShutdownDistance;
                    accel -= physics.LinearVelocity * SettleShutdownMultiplier * scaling;
                }
                physics.WakeBody();
                var impulse = accel * physics.Mass * frameTime;
                physics.ApplyLinearImpulse(impulse);

                if (EntityManager.TryGetComponent<PhysicsComponent?>(puller, out var pullerPhysics))
                {
                    pullerPhysics.WakeBody();
                    pullerPhysics.ApplyLinearImpulse(-impulse);
                }
            }
        }
    }
}
