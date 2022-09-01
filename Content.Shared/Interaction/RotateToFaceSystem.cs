using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Rotatable;
using JetBrains.Annotations;
using Content.Shared.MobState.Components;
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

        public bool TryFaceCoordinates(EntityUid user, Vector2 coordinates)
        {
            var diff = coordinates - EntityManager.GetComponent<TransformComponent>(user).MapPosition.Position;
            if (diff.LengthSquared <= 0.01f)
                return true;
            var diffAngle = Angle.FromWorldVec(diff);
            return TryFaceAngle(user, diffAngle);
        }

        public bool TryFaceAngle(EntityUid user, Angle diffAngle)
        {
            if (_actionBlockerSystem.CanChangeDirection(user) && !_mobState.IsIncapacitated(user))
            {
                Transform(user).WorldRotation = diffAngle;
                return true;
            }
            else
            {
                if (EntityManager.TryGetComponent(user, out SharedBuckleComponent? buckle) && buckle.Buckled)
                {
                    var suid = buckle.LastEntityBuckledTo;
                    if (suid != null)
                    {
                        // We're buckled to another object. Is that object rotatable?
                        if (EntityManager.TryGetComponent<RotatableComponent>(suid.Value, out var rotatable) && rotatable.RotateWhileAnchored)
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
            }
            return false;
        }
    }
}
