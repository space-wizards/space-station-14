using Robust.Shared.Containers;

namespace Content.Server.Containers
{
    public static class ContainerExt
    {
        /// <summary>
        /// Searches the entity, and the entities containers recursively for a prototypeId
        /// </summary>
        /// <param name="entityUid">The entity to search</param>
        /// <param name="prototypeId">The prototypeId to find</param>
        /// <param name="entityManager">Optional entity manager</param>
        /// <returns>True if entity is, or contains a prototype Id</returns>
        public static bool ContainsPrototypeRecursive(this EntityUid entityUid, string prototypeId, IEntityManager? entityManager = null)
        {
            IoCManager.Resolve(ref entityManager);
            var metaQuery = entityManager.GetEntityQuery<MetaDataComponent>();
            var managerQuery = entityManager.GetEntityQuery<ContainerManagerComponent>();
            var stack = new Stack<ContainerManagerComponent>();
            if (metaQuery.GetComponent(entityUid).EntityPrototype?.ID == prototypeId)
                return true;
            if (!managerQuery.TryGetComponent(entityUid, out var currentManager))
                return false;
            do
            {
                foreach (var container in currentManager.Containers.Values)
                {
                    foreach (var entity in container.ContainedEntities)
                    {
                        if (metaQuery.GetComponent(entity).EntityPrototype?.ID == prototypeId)
                            return true;
                        if (!managerQuery.TryGetComponent(entity, out var containerManager))
                            continue;
                        stack.Push(containerManager);
                    }
                }
            } while (stack.TryPop(out currentManager));

            return false;
        }
    }
}
