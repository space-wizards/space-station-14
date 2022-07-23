using Robust.Shared.Containers;

namespace Content.Server.Containers
{
    public static class ContainerExt
    {
        public static int CountPrototypeOccurencesRecursive(this ContainerManagerComponent mgr, string prototypeId)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            int total = 0;
            foreach (var container in mgr.GetAllContainers())
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (entMan.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID == prototypeId) total++;
                    if(!entMan.TryGetComponent<ContainerManagerComponent?>(entity, out var component)) continue;
                    total += component.CountPrototypeOccurencesRecursive(prototypeId);
                }
            }

            return total;
        }
    }
}
