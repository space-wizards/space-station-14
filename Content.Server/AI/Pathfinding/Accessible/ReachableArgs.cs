using Content.Server.AI.Components;
using Content.Server.AI.EntitySystems;
using Content.Shared.Access.Systems;
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
        public static ReachableArgs GetArgs(EntityUid entity)
        {
            var collisionMask = 0;
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (entMan.TryGetComponent(entity, out IPhysBody? physics))
            {
                collisionMask = physics.CollisionMask;
            }

            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            var access = accessSystem.FindAccessTags(entity);
            var visionRadius = entMan.GetComponent<NPCComponent>(entity).VisionRadius;

            return new ReachableArgs(visionRadius, access, collisionMask);
        }
    }
}
