using System.Collections.Generic;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.AI.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.AI.Pathfinding.Accessible
{
    public sealed class ReachableArgs
    {
        public float VisionRadius { get; set; }
        public ICollection<string> Access { get; }
        public int CollisionMask { get; }

        public ReachableArgs(float visionRadius, ICollection<string> access, int collisionMask)
        {
            VisionRadius = visionRadius;
            Access = access;
            CollisionMask = collisionMask;
        }

        /// <summary>
        /// Get appropriate args for a particular entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ReachableArgs GetArgs(IEntity entity)
        {
            var collisionMask = 0;
            if (entity.TryGetComponent(out IPhysBody? physics))
            {
                collisionMask = physics.CollisionMask;
            }

            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            var access = accessSystem.FindAccessTags(entity.Uid);
            var visionRadius = entity.GetComponent<AiControllerComponent>().VisionRadius;

            return new ReachableArgs(visionRadius, access, collisionMask);
        }
    }
}
