using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Rotatable;
using JetBrains.Annotations;

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Contains common code used to rotate a player to face a given target or direction.
    /// This interaction in itself is useful for various roleplay purposes.
    /// But it needs specialized code to handle chairs and such.
    /// Doesn't really fit with SharedInteractionSystem so it's not there.
    /// </summary>
    [UsedImplicitly]
    public sealed class RotateToFaceSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        /// <summary>
        /// Tries to rotate the entity towards the target rotation. Returns false if it needs to keep rotating.
        /// </summary>
        public bool TryRotateTo(EntityUid uid,
            Angle goalRotation,
            float frameTime,
            Angle tolerance,
            double rotationSpeed = float.MaxValue,
            TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref xform))
                return true;

            // If we have a max rotation speed then do that.
            // We'll rotate even if we can't shoot, looks better.
            if (rotationSpeed < float.MaxValue)
            {
                var worldRot = _transform.GetWorldRotation(xform);

                var rotationDiff = Angle.ShortestDistance(worldRot, goalRotation).Theta;
                var maxRotate = rotationSpeed * frameTime;

                if (Math.Abs(rotationDiff) > maxRotate)
                {
                    var goalTheta = worldRot + Math.Sign(rotationDiff) * maxRotate;
                    TryFaceAngle(uid, goalTheta, xform);
                    rotationDiff = (goalRotation - goalTheta);

                    if (Math.Abs(rotationDiff) > tolerance)
                    {
                        return false;
                    }

                    return true;
                }

                TryFaceAngle(uid, goalRotation, xform);
            }
            else
            {
                TryFaceAngle(uid, goalRotation, xform);
            }

            return true;
        }

        public bool TryFaceCoordinates(EntityUid user, Vector2 coordinates, TransformComponent? xform = null)
        {
            if (!Resolve(user, ref xform))
                return false;

            var diff = coordinates - _transform.GetMapCoordinates(user, xform: xform).Position;
            if (diff.LengthSquared() <= 0.01f)
                return true;

            var diffAngle = Angle.FromWorldVec(diff);
            return TryFaceAngle(user, diffAngle);
        }

        public bool TryFaceAngle(EntityUid user, Angle diffAngle, TransformComponent? xform = null)
        {
            if (!_actionBlockerSystem.CanChangeDirection(user))
                return false;

            if (TryComp(user, out BuckleComponent? buckle) && buckle.BuckledTo is {} strap)
            {
                // What if a person is strapped to a borg?
                // I'm pretty sure this would allow them to be partially ratatouille'd

                // We're buckled to another object. Is that object rotatable?
                if (!TryComp<RotatableComponent>(strap, out var rotatable) || !rotatable.RotateWhileAnchored)
                    return false;

                // Note the assumption that even if unanchored, user can only do spinnychair with an "independent wheel".
                // (Since the user being buckled to it holds it down with their weight.)
                // This is logically equivalent to RotateWhileAnchored.
                // Barstools and office chairs have independent wheels, while regular chairs don't.
                _transform.SetWorldRotation(Transform(strap), diffAngle);
                return true;
            }

            // user is not buckled in; apply to their transform
            if (!Resolve(user, ref xform))
                return false;

            _transform.SetWorldRotation(xform, diffAngle);
            return true;
        }
    }
}
