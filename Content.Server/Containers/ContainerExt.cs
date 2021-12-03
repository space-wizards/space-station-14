using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Containers
{
    public static class ContainerExt
    {
        public static int CountPrototypeOccurencesRecursive(this ContainerManagerComponent mgr, string prototypeId)
        {
            int total = 0;
            foreach (var container in mgr.GetAllContainers())
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityPrototype?.ID == prototypeId) total++;
                    if(!entity.TryGetComponent<ContainerManagerComponent>(out var component)) continue;
                    total += component.CountPrototypeOccurencesRecursive(prototypeId);
                }
            }

            return total;
        }
    }
}
