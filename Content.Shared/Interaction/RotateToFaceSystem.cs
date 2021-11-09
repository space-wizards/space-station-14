using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Rotatable;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Contains common code used to rotate a player to face a given target or direction.
    /// This interaction in itself is useful for various roleplay purposes.
    /// But it needs specialized code to handle chairs and such.
    /// Doesn't really fit with SharedInteractionSystem so it's not there.
    /// </summary>
    [UsedImplicitly]
    public class RotateToFaceSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        public bool TryFaceCoordinates(IEntity user, Vector2 coordinates)
        {
            var diff = coordinates - user.Transform.MapPosition.Position;
            if (diff.LengthSquared <= 0.01f)
                return true;
            var diffAngle = Angle.FromWorldVec(diff);
            return TryFaceAngle(user, diffAngle);
        }

        public bool TryFaceAngle(IEntity user, Angle diffAngle)
        {
            if (_actionBlockerSystem.CanChangeDirection(user.Uid))
            {
                user.Transform.WorldRotation = diffAngle;
                return true;
            }
            else
            {
                if (user.TryGetComponent(out SharedBuckleComponent? buckle) && buckle.Buckled)
                {
                    var suid = buckle.LastEntityBuckledTo;
                    if (suid != null)
                    {
                        // We're buckled to another object. Is that object rotatable?
                        if (EntityManager.TryGetComponent<RotatableComponent>(suid.Value!, out var rotatable) && rotatable.RotateWhileAnchored)
                        {
                            // Note the assumption that even if unanchored, user can only do spinnychair with an "independent wheel".
                            // (Since the user being buckled to it holds it down with their weight.)
                            // This is logically equivalent to RotateWhileAnchored.
                            // Barstools and office chairs have independent wheels, while regular chairs don't.
                            rotatable.Owner.Transform.WorldRotation = diffAngle;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
