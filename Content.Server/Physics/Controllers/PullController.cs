using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Gravity;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Rotatable;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    public sealed class PullController : VirtualController
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
        private const float MaximumSettleDistance = 0.1f;
        // Settle shutdown control.
        // Mustn't be too massive, as that causes severe mispredicts *and can prevent it ever resolving*.
        // Exists to bleed off "I pulled my crowbar" overshoots.
        // Minimum velocity for shutdown to be necessary. This prevents stuff getting stuck b/c too much shutdown.
        private const float SettleMinimumShutdownVelocity = 0.25f;
        // Distance in which settle shutdown multiplier is at 0. It then scales upwards linearly with closer distances.
        private const float SettleShutdownDistance = 1.0f;
        // Velocity change of -LinearVelocity * frameTime * this
        private const float SettleShutdownMultiplier = 20.0f;

        // How much you must move for the puller movement check to actually hit.
        private const float MinimumMovementDistance = 0.005f;

        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedPullingSystem _pullableSystem = default!;
        [Dependency] private readonly SharedGravitySystem _gravity = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        // TODO: Move this stuff to pullingsystem
        /// <summary>
        ///     If distance between puller and pulled entity lower that this threshold,
        ///     pulled entity will not change its rotation.
        ///     Helps with small distance jittering
        /// </summary>
        private const float ThresholdRotDistance = 1;

        /// <summary>
        ///     If difference between puller and pulled angle  lower that this threshold,
        ///     pulled entity will not change its rotation.
        ///     Helps with diagonal movement jittering
        ///     As of further adjustments, should divide cleanly into 90 degrees
        /// </summary>
        private const float ThresholdRotAngle = 22.5f;

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(MoverController));
            SubscribeLocalEvent<SharedPullerComponent, MoveEvent>(OnPullerMove);

            base.Initialize();
        }

        private void OnPullerMove(EntityUid uid, SharedPullerComponent component, ref MoveEvent args)
        {
            if (component.Pulling is not { } pullable || !TryComp<SharedPullableComponent>(pullable, out var pullableComponent))
                return;

            UpdatePulledRotation(uid, pullable);

            if (args.NewPosition.EntityId == args.OldPosition.EntityId &&
                (args.NewPosition.Position - args.OldPosition.Position).LengthSquared() < MinimumMovementDistance * MinimumMovementDistance)
                return;

            if (TryComp<PhysicsComponent>(pullable, out var physics))
                PhysicsSystem.WakeBody(pullable, body: physics);

            _pullableSystem.StopMoveTo(pullableComponent);
        }

        private void UpdatePulledRotation(EntityUid puller, EntityUid pulled)
        {
            // TODO: update once ComponentReference works with directed event bus.
            if (!TryComp(pulled, out RotatableComponent? rotatable))
                return;

            if (!rotatable.RotateWhilePulling)
                return;

            var xforms = GetEntityQuery<TransformComponent>();
            var pulledXform = xforms.GetComponent(pulled);
            var pullerXform = xforms.GetComponent(puller);

            var pullerData = TransformSystem.GetWorldPositionRotation(pullerXform, xforms);
            var pulledData = TransformSystem.GetWorldPositionRotation(pulledXform, xforms);

            var dir = pullerData.WorldPosition - pulledData.WorldPosition;
            if (dir.LengthSquared() > ThresholdRotDistance * ThresholdRotDistance)
            {
                var oldAngle = pulledData.WorldRotation;
                var newAngle = Angle.FromWorldVec(dir);

                var diff = newAngle - oldAngle;
                if (Math.Abs(diff.Degrees) > ThresholdRotAngle / 2f)
                {
                    // Ok, so this bit is difficult because ideally it would look like it's snapping to sane angles.
                    // Otherwise PIANO DOOR STUCK! happens.
                    // But it also needs to work with station rotation / align to the local parent.
                    // So...
                    var baseRotation = pulledData.WorldRotation - pulledXform.LocalRotation;
                    var localRotation = newAngle - baseRotation;
                    var localRotationSnapped = Angle.FromDegrees(Math.Floor((localRotation.Degrees / ThresholdRotAngle) + 0.5f) * ThresholdRotAngle);
                    TransformSystem.SetLocalRotation(pulledXform, localRotationSnapped);
                }
            }
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
                    continue;

                if (pullable.MovingTo == null)
                    continue;

                if (pullable.Puller is not {Valid: true} puller)
                    continue;

                var pullableEnt = pullable.Owner;
                var pullableXform = Transform(pullableEnt);
                var pullerXform = Transform(puller);

                // Now that's over with...

                var pullerPosition = pullerXform.MapPosition;
                var movingTo = pullable.MovingTo.Value.ToMap(EntityManager, _transform);
                if (movingTo.MapId != pullerPosition.MapId)
                {
                    _pullableSystem.StopMoveTo(pullable);
                    continue;
                }

                if (!TryComp<PhysicsComponent>(pullableEnt, out var physics) ||
                    physics.BodyType == BodyType.Static ||
                    movingTo.MapId != pullableXform.MapID)
                {
                    _pullableSystem.StopMoveTo(pullable);
                    continue;
                }

                var movingPosition = movingTo.Position;
                var ownerPosition = pullableXform.MapPosition.Position;

                var diff = movingPosition - ownerPosition;
                var diffLength = diff.Length();

                if (diffLength < MaximumSettleDistance && physics.LinearVelocity.Length() < MaximumSettleVelocity)
                {
                    PhysicsSystem.SetLinearVelocity(pullableEnt, Vector2.Zero, body: physics);
                    _pullableSystem.StopMoveTo(pullable);
                    continue;
                }

                var impulseModifierLerp = Math.Min(1.0f, Math.Max(0.0f, (physics.Mass - AccelModifierLowMass) / (AccelModifierHighMass - AccelModifierLowMass)));
                var impulseModifier = MathHelper.Lerp(AccelModifierLow, AccelModifierHigh, impulseModifierLerp);
                var multiplier = diffLength < 1 ? impulseModifier * diffLength : impulseModifier;
                // Note the implication that the real rules of physics don't apply to pulling control.
                var accel = diff.Normalized() * multiplier;
                // Now for the part where velocity gets shutdown...
                if (diffLength < SettleShutdownDistance && physics.LinearVelocity.Length() >= SettleMinimumShutdownVelocity)
                {
                    // Shutdown velocity increases as we get closer to centre
                    var scaling = (SettleShutdownDistance - diffLength) / SettleShutdownDistance;
                    accel -= physics.LinearVelocity * SettleShutdownMultiplier * scaling;
                }

                PhysicsSystem.WakeBody(pullableEnt, body: physics);

                var impulse = accel * physics.Mass * frameTime;
                PhysicsSystem.ApplyLinearImpulse(pullableEnt, impulse, body: physics);

                // if the puller is weightless or can't move, then we apply the inverse impulse (Newton's third law).
                // doing it under gravity produces an unsatisfying wiggling when pulling.
                // If player can't move, assume they are on a chair and we need to prevent pull-moving.
                if ((_gravity.IsWeightless(puller) && pullerXform.GridUid == null) || !_actionBlockerSystem.CanMove(puller))
                {
                    PhysicsSystem.WakeBody(puller);
                    PhysicsSystem.ApplyLinearImpulse(puller, -impulse);
                }
            }
        }
    }
}
