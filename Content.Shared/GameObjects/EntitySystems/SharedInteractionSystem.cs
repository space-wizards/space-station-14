using System;
using System.Linq;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public class SharedInteractionSystem : EntitySystem
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        /// <summary>
        ///     Traces a ray from coords to otherCoords and returns the length
        ///     of the vector between coords and the ray's first hit.
        /// </summary>
        /// <param name="coords">Set of coordinates to use.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="collisionMask">the mask to check for collisions</param>
        /// <param name="predicate">A predicate to check whether to ignore an entity or not. If it returns true, it will be ignored.</param>
        /// <returns>Length of resulting ray.</returns>
        public float UnobstructedRayLength(MapCoordinates coords, MapCoordinates otherCoords,
            int collisionMask = (int) CollisionGroup.Impassable, Func<IEntity, bool> predicate = null)
        {
            var dir = otherCoords.Position - coords.Position;

            if (dir.LengthSquared.Equals(0f)) return 0f;

            var ray = new CollisionRay(coords.Position, dir.Normalized, collisionMask);
            var rayResults = _physicsManager.IntersectRayWithPredicate(coords.MapId, ray, dir.Length, predicate, returnOnFirstHit: false).ToList();

            if (rayResults.Count == 0) return dir.Length;
            return (rayResults[0].HitPos - coords.Position).Length;
        }

        /// <summary>
        ///     Traces a ray from coords to otherCoords and returns the length
        ///     of the vector between coords and the ray's first hit.
        /// </summary>
        /// <param name="coords">Set of coordinates to use.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="collisionMask">the mask to check for collisions</param>
        /// <param name="ignoredEnt">the entity to be ignored when checking for collisions.</param>
        /// <returns>Length of resulting ray.</returns>
        public float UnobstructedRayLength(MapCoordinates coords, MapCoordinates otherCoords,
            int collisionMask = (int) CollisionGroup.Impassable, IEntity ignoredEnt = null) =>
            UnobstructedRayLength(coords, otherCoords, collisionMask,
                ignoredEnt == null ? null : (Func<IEntity, bool>) (entity => ignoredEnt == entity));

        /// <summary>
        ///     Checks that these coordinates are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two sets of coordinates..
        /// </summary>
        /// <param name="coords">Set of coordinates to use.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="range">maximum distance between the two sets of coordinates.</param>
        /// <param name="collisionMask">the mask to check for collisions</param>
        /// <param name="predicate">A predicate to check whether to ignore an entity or not. If it returns true, it will be ignored.</param>
        /// <param name="ignoreInsideBlocker">if true and the coordinates are inside the obstruction, ignores the obstruction and
        /// considers the interaction unobstructed. Therefore, setting this to true makes this check more permissive, such
        /// as allowing an interaction to occur inside something impassable (like a wall). The default, false,
        /// makes the check more restrictive.</param>
        /// <returns>True if the two points are within a given range without being obstructed.</returns>
        public bool InRangeUnobstructed(MapCoordinates coords, MapCoordinates otherCoords, float range = InteractionRange,
            int collisionMask = (int)CollisionGroup.Impassable, Func<IEntity, bool> predicate = null, bool ignoreInsideBlocker = false)
        {
            if (!coords.InRange(otherCoords, range))
                return false;

            var dir = otherCoords.Position - coords.Position;

            if (dir.LengthSquared.Equals(0f)) return true;
            if (range > 0f && !(dir.LengthSquared <= range * range)) return false;

            var ray = new CollisionRay(coords.Position, dir.Normalized, collisionMask);
            var rayResults = _physicsManager.IntersectRayWithPredicate(coords.MapId, ray, dir.Length, predicate, returnOnFirstHit: false).ToList();
            return rayResults.Count == 0 || (ignoreInsideBlocker && rayResults.Count > 0 && (rayResults[0].HitPos - otherCoords.Position).Length < 1f);
        }

        /// <summary>
        ///     Checks that these coordinates are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two sets of coordinates..
        /// </summary>
        /// <param name="coords">Set of coordinates to use.</param>
        /// <param name="otherCoords">Other set of coordinates to use.</param>
        /// <param name="range">maximum distance between the two sets of coordinates.</param>
        /// <param name="collisionMask">the mask to check for collisions</param>
        /// <param name="ignoredEnt">the entity to be ignored when checking for collisions.</param>
        /// <param name="ignoreInsideBlocker">if true and the coordinates are inside the obstruction, ignores the obstruction and
        /// considers the interaction unobstructed. Therefore, setting this to true makes this check more permissive, such
        /// as allowing an interaction to occur inside something impassable (like a wall).  The default, false,
        /// makes the check more restrictive.</param>
        /// <returns>True if the two points are within a given range without being obstructed.</returns>
        public bool InRangeUnobstructed(MapCoordinates coords, MapCoordinates otherCoords, float range = InteractionRange,
            int collisionMask = (int)CollisionGroup.Impassable, IEntity ignoredEnt = null, bool ignoreInsideBlocker = false) =>
            InRangeUnobstructed(coords, otherCoords, range, collisionMask,
                ignoredEnt == null ? null : (Func<IEntity, bool>)(entity => ignoredEnt == entity), ignoreInsideBlocker);
    }
}
