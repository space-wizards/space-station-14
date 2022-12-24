using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Rotatable;
using JetBrains.Annotations;
using Content.Shared.MobState.EntitySystems;

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
        [Dependency] private readonly SharedMobStateSystem _mobState = default!;
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
                    _transform.SetWorldRotation(xform, goalTheta);
                    rotationDiff = (goalRotation - goalTheta);

                    if (Math.Abs(rotationDiff) > tolerance)
                    {
                        return false;
                    }

                    return true;
                }

                _transform.SetWorldRotation(xform, goalRotation);
            }
            else
            {
                _transform.SetWorldRotation(xform, goalRotation);
            }

            return true;
        }

        public bool TryFaceCoordinates(EntityUid user, Vector2 coordinates, TransformComponent? xform = null)
        {
            if (!Resolve(user, ref xform))
                return false;

            var diff = coordinates - xform.MapPosition.Position;
            if (diff.LengthSquared <= 0.01f)
                return true;

            var diffAngle = Angle.FromWorldVec(diff);
            return TryFaceAngle(user, diffAngle);
        }

        public bool TryFaceAngle(EntityUid user, Angle diffAngle, TransformComponent? xform = null)
        {
            if (_actionBlockerSystem.CanChangeDirection(user))
            {
                if (!Resolve(user, ref xform))
                    return false;

                xform.WorldRotation = diffAngle;
                return true;
            }

            if (EntityManager.TryGetComponent(user, out BuckleComponent? buckle) && buckle.Buckled)
            {
                var suid = buckle.LastEntityBuckledTo;
                if (suid != null)
                {
                    // We're buckled to another object. Is that object rotatable?
                    if (TryComp<RotatableComponent>(suid.Value!, out var rotatable) && rotatable.RotateWhileAnchored)
                    {
                        // Note the assumption that even if unanchored, user can only do spinnychair with an "independent wheel".
                        // (Since the user being buckled to it holds it down with their weight.)
                        // This is logically equivalent to RotateWhileAnchored.
                        // Barstools and office chairs have independent wheels, while regular chairs don't.
                        Transform(rotatable.Owner).WorldRotation = diffAngle;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
